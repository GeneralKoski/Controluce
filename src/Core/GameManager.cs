using System.Threading.Tasks;
using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Core;

public partial class GameManager : Node
{
    [Signal] public delegate void RoomLoadedEventHandler(int index);

    [Export] public PlayerController? Player1 { get; set; }
    [Export] public PlayerController? Player2 { get; set; }
    [Export] public Label? Banner { get; set; }
    [Export] public ColorRect? Fade { get; set; }
    [Export] public RopeVisual? Rope { get; set; }
    [Export] public Node3D? RoomSlot { get; set; }
    [Export] public string[] RoomPaths { get; set; } = [];
    [Export] public float KillY { get; set; } = -8f;
    // Anti-rage: di default respawnano entrambi insieme, all'ultimo checkpoint.
    [Export] public bool RespawnBothPlayers { get; set; } = true;

    private int _roomIndex;
    private Node3D? _currentRoom;
    private bool _transitioning;
    private Vector3 _spawnA;
    private Vector3 _spawnB;
    private Checkpoint? _activeCheckpoint;

    public int CurrentRoomIndex => _roomIndex;

    public override void _Ready()
    {
        // Il mondo 3D vive nel viewport root ed è condiviso dai due SubViewport:
        // il root non deve renderizzarlo (lo coprono i due schermi).
        GetViewport().Disable3D = true;

        var music = new AudioStreamPlayer
        {
            Stream = AudioSynth.AmbientPad(),
            VolumeDb = -18f,
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(music);
        music.Play();

        LoadRoom(0);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Player1 == null || Player2 == null || _transitioning)
            return;

        bool fellA = Player1.GlobalPosition.Y < KillY;
        bool fellB = Player2.GlobalPosition.Y < KillY;
        if (!fellA && !fellB)
            return;

        if (RespawnBothPlayers || (fellA && fellB))
        {
            Respawn(Player1, _spawnA);
            Respawn(Player2, _spawnB);
        }
        else if (fellA)
            Respawn(Player1, _spawnA);
        else
            Respawn(Player2, _spawnB);

        Rope?.Snap();
    }

    public void LoadRoom(int index)
    {
        if (RoomSlot == null || RoomPaths.Length == 0 || index >= RoomPaths.Length)
            return;

        _currentRoom?.Free();
        _roomIndex = index;
        _currentRoom = GD.Load<PackedScene>(RoomPaths[index]).Instantiate<Node3D>();
        RoomSlot.AddChild(_currentRoom);
        _activeCheckpoint = null;

        foreach (Node node in GetTree().GetNodesInGroup("exit_zone"))
        {
            if (node is ExitZone exit)
                exit.RoomCompleted += OnRoomCompleted;
        }

        foreach (Node node in GetTree().GetNodesInGroup("checkpoint"))
        {
            if (node is Checkpoint checkpoint)
                checkpoint.Activated += OnCheckpointActivated;
        }

        _spawnA = SpawnPosition(Player1, "spawn_a");
        _spawnB = SpawnPosition(Player2, "spawn_b");
        if (Player1 != null)
            Respawn(Player1, _spawnA);
        if (Player2 != null)
            Respawn(Player2, _spawnB);
        Rope?.Snap();

        EmitSignal(SignalName.RoomLoaded, index);
    }

    public void RestartRoom() => LoadRoom(_roomIndex);

    private static void Respawn(PlayerController player, Vector3 position)
    {
        player.GlobalPosition = position;
        player.Velocity = Vector3.Zero;
    }

    private async void OnRoomCompleted()
    {
        if (_transitioning)
            return;

        bool isLast = _roomIndex + 1 >= RoomPaths.Length;
        ShowBanner(isLast ? "Hai completato Controluce!" : "Stanza completata!");
        if (isLast)
            return;

        _transitioning = true;
        await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
        await FadeTo(1f);
        if (Banner != null)
            Banner.Visible = false;
        LoadRoom(_roomIndex + 1);
        await FadeTo(0f);
        _transitioning = false;
    }

    private async Task FadeTo(float alpha)
    {
        if (Fade == null)
            return;

        var tween = CreateTween();
        tween.TweenProperty(Fade, "modulate:a", alpha, 0.6f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void ShowBanner(string text)
    {
        GD.Print(text);
        if (Banner != null)
        {
            Banner.Text = text;
            Banner.Visible = true;
        }
    }

    private void OnCheckpointActivated(Checkpoint checkpoint)
    {
        if (_activeCheckpoint == checkpoint)
            return;

        _activeCheckpoint?.SetActive(false);
        _activeCheckpoint = checkpoint;
        checkpoint.SetActive(true);
        _spawnA = checkpoint.SpawnPositionA;
        _spawnB = checkpoint.SpawnPositionB;
    }

    private Vector3 SpawnPosition(PlayerController? player, string group)
    {
        if (GetTree().GetFirstNodeInGroup(group) is Node3D spawn)
            return spawn.GlobalPosition;
        return player?.GlobalPosition ?? Vector3.Zero;
    }
}
