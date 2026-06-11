using Controluce.Core;
using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Test integrato della stanza 6 "Il traghettatore": P2 sulla pedana manda
// su l'ascensore con P1; i due scendono insieme (gradini blu / traghetto
// autonomo); pedana finale a due e corsa oltre la porta a tempo.
public partial class Room06Test : Node
{
    private GameManager _gm = null!;
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private MovingPlatform _ponte = null!;
    private MovingPlatform _porta = null!;
    private int _ticks;
    private bool _failed;

    // Fasi della "partita": 0 salita, 1 traversata in quota, 2 imbarco,
    // 3 traghettata+discesa, 4 pedana finale, 5 corsa all'uscita.
    private int _stage;
    private bool _p1Lifted;
    private bool _boarded;
    private bool _exitSeen;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _gm = GetNode<GameManager>("Main");
        _p1 = GetNode<PlayerController>("Main/World/Player1");
        _p2 = GetNode<PlayerController>("Main/World/Player2");
        _p1.GetNode("PlayerInput").QueueFree();
        _p2.GetNode("PlayerInput").QueueFree();
    }

    private static Vector2 Hold(PlayerController player, float x, float z) =>
        new Vector2(
            Mathf.Clamp(x - player.GlobalPosition.X, -1f, 1f),
            Mathf.Clamp(z - player.GlobalPosition.Z, -1f, 1f)).LimitLength(1f);

    private void Command(PlayerController player, float x, float z) =>
        player.SetCommand(new PlayerCommand(Hold(player, x, z), false, false, false));

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 10)
        {
            _gm.LoadRoom(5);
            _ponte = GetNode<MovingPlatform>("Main/World/RoomSlot/Room06/Ponte");
            _porta = GetNode<MovingPlatform>("Main/World/RoomSlot/Room06/Porta");
        }

        if (_ticks == 30)
        {
            _p1.GlobalPosition = new Vector3(1f, 1f, -7f);
            _p1.Velocity = Vector3.Zero;
            _p2.GlobalPosition = new Vector3(-1.5f, 1f, -4.5f);
            _p2.Velocity = Vector3.Zero;
        }

        if (_ticks <= 30)
            return;

        if (_ticks % 120 == 0)
            GD.Print($"t{_ticks} stage{_stage} P1 {_p1.GlobalPosition} P2 {_p2.GlobalPosition} asc {GetNode<Node3D>("Main/World/RoomSlot/Room06/Ascensore").GlobalPosition.Y:F2}");

        switch (_stage)
        {
            case 0: // P2 tiene la pedana, P1 sta sull'ascensore che sale.
                Command(_p1, 1f, -7f);
                Command(_p2, -1.5f, -4.5f);
                if (_p1.GlobalPosition.Y > 2.6f)
                {
                    _p1Lifted = true;
                    _stage = 1;
                }
                break;

            case 1: // P1 cammina sulla cengia; P2 lo segue sotto, fino al molo.
                Command(_p1, 1f, -17f);
                if (_p1.GlobalPosition.Z < -9f)
                    Command(_p2, -1.5f, -17f);
                else
                    Command(_p2, -1.5f, -4.5f);
                if (_p1.GlobalPosition.Z < -16.5f && _p2.GlobalPosition.Z < -16.5f)
                {
                    Check(_p1.GlobalPosition.Y > 2.5f, "P1 è arrivato in quota sull'ascensore");
                    _stage = 2;
                }
                break;

            case 2: // P2 sale sul traghetto quando è accostato al molo.
                Command(_p1, 1f, -17.5f);
                if (_ponte.GlobalPosition.Z > -20.15f || _boarded)
                {
                    _boarded = true;
                    Command(_p2, -1.5f, -20f);
                }
                else
                    Command(_p2, -1.5f, -17f);
                if (_boarded && _p2.GlobalPosition.Z < -19.2f && _p2.IsOnFloor())
                    _stage = 3;
                break;

            case 3: // Il traghetto porta P2; P1 scende i gradini blu in parallelo.
                Command(_p2, -1.5f, _p2.GlobalPosition.Z);
                Command(_p1, 1f, -22f);
                if (_p2.GlobalPosition.Z < -22f)
                {
                    Check(true, "il traghetto ha portato P2 oltre il vuoto");
                    _stage = 4;
                }
                break;

            case 4: // Entrambi sulla pedana finale.
                Command(_p1, 0.45f, -28.5f);
                Command(_p2, -0.45f, -28.5f);
                if (_porta.GlobalPosition.Y < -1f)
                {
                    Check(true, "la pedana a due ha abbassato la porta");
                    _stage = 5;
                }
                break;

            case 5: // Corsa all'uscita prima che la porta risalga.
                Command(_p1, 0.8f, -33.5f);
                Command(_p2, -0.8f, -33.5f);
                break;
        }

        // Il banner di completamento viene poi nascosto dalla schermata di
        // finale: va colto al volo, non controllato a fine corsa.
        var bannerLabel = GetNode<Label>("Main/UI/Banner");
        if (bannerLabel.Visible && bannerLabel.Text.Contains("completat"))
            _exitSeen = true;

        if (_ticks == 2400)
        {
            Check(_p1Lifted, "l'ascensore ha sollevato P1");
            Check(_stage == 5, $"sequenza completata (fase {_stage})");
            Check(_exitSeen, "uscita scattata con entrambi dentro");
            Check(GetNode("Main/UI").HasNode("Finale"),
                "la schermata di finale è comparsa dopo l'ultima stanza");

            GD.Print(_failed ? "ROOM06 TEST: FAIL" : "ROOM06 TEST: PASS");
            GetTree().Quit(_failed ? 1 : 0);
        }
    }

    private void Check(bool condition, string label)
    {
        GD.Print($"{(condition ? "OK " : "FAIL")} - {label}");
        if (!condition)
            _failed = true;
    }
}
