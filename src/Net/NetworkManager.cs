using System.Collections.Generic;
using Controluce.Core;
using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Net;

// Co-op online con server authoritative (il server è anche il Player 1).
// La simulazione (player, corda, respawn, stanze) gira SOLO sul server;
// il client invia i propri PlayerCommand e renderizza gli snapshot ricevuti.
//
// Avvio (nessun segreto nel sorgente: porta/host via argomenti o env):
//   server: godot-mono --path . -- --server [porta]
//   client: godot-mono --path . -- --client [host] [porta]
//   oppure CONTROLUCE_MODE=server|client, CONTROLUCE_HOST, CONTROLUCE_PORT
public partial class NetworkManager : Node
{
    private enum Mode { Local, Server, Client }

    private const double InterpolationDelay = 0.1;
    private const double SnapshotInterval = 1.0 / 30.0;

    [Export] public GameManager? Game { get; set; }
    [Export] public PlayerController? Player1 { get; set; }
    [Export] public PlayerController? Player2 { get; set; }
    [Export] public RopeConstraint? Rope { get; set; }
    [Export] public Control? ViewP1 { get; set; }
    [Export] public Control? ViewP2 { get; set; }

    // Per i test: sostituisce la lettura dell'input locale del client.
    public System.Func<PlayerCommand>? LocalCommandOverride { get; set; }


    private Mode _mode = Mode.Local;
    private int _clientSkin2 = -1;
    private CameraRig? _clientCamera;
    private Vector2 _remoteMove;
    private bool _remoteJump;
    private bool _remotePing;
    private bool _remotePull;
    private double _sendTimer;

    private record struct Snap(
        double Time, int Room,
        Vector3 P1Pos, Vector3 P1Vel,
        Vector3 P2Pos, Vector3 P2Vel,
        float RopeLength, float RopeTension,
        Vector3[] Movers);

    private readonly List<Snap> _buffer = [];
    private readonly List<MovingPlatform> _movers = [];

    public override void _Ready()
    {
        // Il server consegna il comando remoto a P2 prima dello step dei player.
        ProcessPhysicsPriority = -5;

        Settings.Load();
        ParseConfig(out string host, out int port);
        if (_mode == Mode.Local)
            return;

        // L'elenco dei mover cambia a ogni stanza (l'ordine dell'albero è
        // identico sui due peer, quindi gli indici coincidono).
        if (Game != null)
            Game.RoomLoaded += _ => RefreshMovers();

        var peer = new ENetMultiplayerPeer();
        if (_mode == Mode.Server)
        {
            Error error = peer.CreateServer(port, maxClients: 1);
            if (error != Error.Ok)
            {
                GD.PushError($"Impossibile aprire il server sulla porta {port}: {error}");
                return;
            }
            Multiplayer.MultiplayerPeer = peer;
            GD.Print($"Server Controluce in ascolto sulla porta {port}");
            Game?.ShowBanner($"In attesa dell'ospite sulla porta {port}...");
            Multiplayer.PeerConnected += _ => Game?.HideBanner();
            Multiplayer.PeerDisconnected += _ =>
                Game?.ShowBanner($"Ospite disconnesso: in attesa sulla porta {port}...");

            // P2 è il giocatore remoto: niente input locale.
            Player2?.GetNodeOrNull("PlayerInput")?.QueueFree();
            if (ViewP2 != null)
                ViewP2.Visible = false;
            DisableViewport(ViewP2, "ViewportP2");

            if (Player1 != null)
                Player1.Pinged += (pos, phase) => Rpc(MethodName.RemotePing, pos, phase);
            if (Player2 != null)
                Player2.Pinged += (pos, phase) => Rpc(MethodName.RemotePing, pos, phase);

            // All'ingresso dell'ospite gli si comunicano skin e ruoli correnti.
            Multiplayer.PeerConnected += _ => SendAppearance();
        }
        else
        {
            Error error = peer.CreateClient(host, port);
            if (error != Error.Ok)
            {
                GD.PushError($"Impossibile connettersi a {host}:{port}: {error}");
                return;
            }
            Multiplayer.MultiplayerPeer = peer;
            GD.Print($"Connessione a {host}:{port}...");
            Game?.ShowBanner($"Connessione a {host}:{port}...");
            Multiplayer.ConnectedToServer += () =>
            {
                GD.Print("Connesso al server");
                Game?.HideBanner();
                // L'ospite gioca P2: annuncia la propria skin P2 al server.
                RpcId(1, MethodName.AnnounceSkin, Settings.SkinP2);
            };
            Multiplayer.ServerDisconnected += ReturnToMenu;
            Multiplayer.ConnectionFailed += ReturnToMenu;

            // Niente simulazione locale: si renderizza lo stato del server.
            Player1?.SetPhysicsProcess(false);
            Player1?.GetNodeOrNull("PlayerInput")?.QueueFree();
            Player2?.SetPhysicsProcess(false);
            Player2?.GetNodeOrNull("PlayerInput")?.QueueFree();
            Rope?.SetPhysicsProcess(false);
            Game?.SetPhysicsProcess(false);
            if (Game != null)
                Game.NetworkPassive = true;
            if (ViewP1 != null)
                ViewP1.Visible = false;
            DisableViewport(ViewP1, "ViewportP1");

            // Il client gioca P2 a schermo pieno con i binding di P1: mouse sulla sua camera.
            _clientCamera = ViewP2?.GetNodeOrNull<CameraRig>("ViewportP2/CameraRig");
            if (_clientCamera != null)
            {
                _clientCamera.UseMouse = true;
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_mode == Mode.Server)
        {
            Player2?.SetCommand(new PlayerCommand(_remoteMove, _remoteJump, _remotePing, _remotePull));
            _remoteJump = false;
            _remotePing = false;
        }
        else if (_mode == Mode.Client && IsConnected())
        {
            PlayerCommand command = LocalCommandOverride?.Invoke() ?? PlayerInput.CaptureFrom("p1", _clientCamera);
            RpcId(1, MethodName.SubmitCommand, command.MoveAxis, command.JumpPressed, command.PingPressed, command.PullHeld);
        }
    }

    public override void _Process(double delta)
    {
        if (_mode == Mode.Server)
        {
            _sendTimer -= delta;
            if (_sendTimer <= 0.0 && Multiplayer.GetPeers().Length > 0
                && Game != null && Player1 != null && Player2 != null && Rope != null)
            {
                _sendTimer = SnapshotInterval;
                var movers = new Vector3[_movers.Count];
                for (int i = 0; i < _movers.Count; i++)
                    movers[i] = _movers[i].GlobalPosition;
                Rpc(MethodName.Snapshot, Game.CurrentRoomIndex,
                    Player1.GlobalPosition, Player1.Velocity,
                    Player2.GlobalPosition, Player2.Velocity,
                    Rope.CurrentLength, Rope.Tension, movers);
            }
        }
        else if (_mode == Mode.Client)
        {
            ApplySnapshots();
        }
    }

    // Un SubViewportContainer nascosto continua a renderizzare il mondo:
    // online la vista dell'altro player va spenta davvero.
    private static void DisableViewport(Control? container, string viewportName)
    {
        if (container?.GetNodeOrNull<SubViewport>(viewportName) is { } viewport)
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
    }

    private bool IsConnected() =>
        Multiplayer.MultiplayerPeer?.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SubmitCommand(Vector2 move, bool jump, bool ping, bool pull)
    {
        if (_mode != Mode.Server)
            return;

        _remoteMove = move;
        _remoteJump |= jump;
        _remotePing |= ping;
        _remotePull = pull;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void Snapshot(int room, Vector3 p1Pos, Vector3 p1Vel, Vector3 p2Pos, Vector3 p2Vel, float ropeLength, float ropeTension, Vector3[] movers)
    {
        _buffer.Add(new Snap(Now(), room, p1Pos, p1Vel, p2Pos, p2Vel, ropeLength, ropeTension, movers));
        if (_buffer.Count > 120)
            _buffer.RemoveRange(0, _buffer.Count - 120);
    }

    // Aspetto concordato: skin P1 e ruoli dell'host, skin P2 dell'ospite.
    private void SendAppearance()
    {
        int skin2 = _clientSkin2 >= 0 ? _clientSkin2 : Settings.SkinP2;
        Game?.ApplyAppearance(Settings.SkinP1, skin2, Settings.SwapRoles);
        Rpc(MethodName.SyncAppearance, Settings.SkinP1, skin2, Settings.SwapRoles);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AnnounceSkin(int skin)
    {
        if (_mode != Mode.Server)
            return;

        _clientSkin2 = skin;
        SendAppearance();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncAppearance(int skin1, int skin2, bool swapRoles)
    {
        if (_mode == Mode.Client)
            Game?.ApplyAppearance(skin1, skin2, swapRoles);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RemotePing(Vector3 position, int phase)
    {
        if (Player1?.GetParent() is not Node parent)
            return;

        var marker = new PingMarker
        {
            Color = (Phase)phase == Phase.Blue ? new Color(0.4f, 0.65f, 1f) : new Color(1f, 0.45f, 0.4f),
        };
        parent.AddChild(marker);
        marker.GlobalPosition = position;
    }

    private void ApplySnapshots()
    {
        if (_buffer.Count == 0 || Player1 == null || Player2 == null)
            return;

        Snap latest = _buffer[^1];
        if (Game != null && latest.Room != Game.CurrentRoomIndex)
            Game.LoadRoom(latest.Room);

        double renderTime = Now() - InterpolationDelay;
        Snap from = _buffer[0];
        Snap to = latest;
        for (int i = 0; i < _buffer.Count - 1; i++)
        {
            if (_buffer[i].Time <= renderTime && _buffer[i + 1].Time >= renderTime)
            {
                from = _buffer[i];
                to = _buffer[i + 1];
                break;
            }
        }

        float t = to.Time > from.Time
            ? (float)Mathf.Clamp((renderTime - from.Time) / (to.Time - from.Time), 0.0, 1.0)
            : 1f;

        Player1.GlobalPosition = from.P1Pos.Lerp(to.P1Pos, t);
        Player1.Velocity = from.P1Vel.Lerp(to.P1Vel, t);
        Player2.GlobalPosition = from.P2Pos.Lerp(to.P2Pos, t);
        Player2.Velocity = from.P2Vel.Lerp(to.P2Vel, t);
        Rope?.ApplyNetworkState(
            Mathf.Lerp(from.RopeLength, to.RopeLength, t),
            Mathf.Lerp(from.RopeTension, to.RopeTension, t));

        int moverCount = Mathf.Min(_movers.Count, Mathf.Min(from.Movers.Length, to.Movers.Length));
        for (int i = 0; i < moverCount; i++)
            _movers[i].GlobalPosition = from.Movers[i].Lerp(to.Movers[i], t);

        while (_buffer.Count > 2 && _buffer[1].Time < renderTime - 1.0)
            _buffer.RemoveAt(0);
    }

    private void ReturnToMenu()
    {
        GD.Print("Connessione persa: ritorno al menu");
        GameConfig.Mode = null;
        Multiplayer.MultiplayerPeer = null;
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
    }

    private void RefreshMovers()
    {
        _movers.Clear();
        foreach (Node node in GetTree().GetNodesInGroup("mover"))
        {
            if (node is not MovingPlatform mover)
                continue;

            _movers.Add(mover);
            // Sul client i mover sono renderizzati dagli snapshot, non simulati.
            if (_mode == Mode.Client)
                mover.SetPhysicsProcess(false);
        }
    }

    private static double Now() => Time.GetTicksMsec() / 1000.0;

    private void ParseConfig(out string host, out int port)
    {
        // Base: scelte fatte nel menu; CLI e variabili d'ambiente le sovrascrivono.
        string mode = GameConfig.Mode ?? "";
        host = GameConfig.Host;
        port = GameConfig.Port;

        string envHost = OS.GetEnvironment("CONTROLUCE_HOST");
        if (!string.IsNullOrEmpty(envHost))
            host = envHost;
        if (int.TryParse(OS.GetEnvironment("CONTROLUCE_PORT"), out int envPort))
            port = envPort;

        string envMode = OS.GetEnvironment("CONTROLUCE_MODE").ToLowerInvariant();
        if (!string.IsNullOrEmpty(envMode))
            mode = envMode;

        string[] args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--server":
                    mode = "server";
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int serverPort))
                    {
                        port = serverPort;
                        i++;
                    }
                    break;
                case "--client":
                    mode = "client";
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        host = args[++i];
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int clientPort))
                        {
                            port = clientPort;
                            i++;
                        }
                    }
                    break;
            }
        }

        _mode = mode switch
        {
            "server" => Mode.Server,
            "client" => Mode.Client,
            _ => Mode.Local,
        };
    }
}
