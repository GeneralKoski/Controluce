using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Test integrato della stanza 4 "Il pendolo": P1 fa da ancora sul ponte
// alto, P2 penzola nel vuoto, pompa il dondolio e salta da appeso fino
// all'approdo; poi l'uscita scatta con entrambi dentro.
public partial class Room04Test : Node
{
    private GameManager _gm = null!;
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private int _ticks;
    private bool _failed;
    private bool _hungObserved;
    private bool _landed;
    private int _jumpCooldown;
    private RopeConstraint _rope = null!;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _gm = GetNode<GameManager>("Main");
        _p1 = GetNode<PlayerController>("Main/World/Player1");
        _p2 = GetNode<PlayerController>("Main/World/Player2");
        _rope = GetNode<RopeConstraint>("Main/World/Rope");
        _p1.GetNode("PlayerInput").QueueFree();
        _p2.GetNode("PlayerInput").QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 10)
            _gm.LoadRoom(3);

        if (_ticks == 30)
        {
            // P1 in fondo al ponte alto, da ancora; P2 al bordo del vuoto.
            _p1.GlobalPosition = new Vector3(-1.5f, 4.5f, -20.5f);
            _p1.Velocity = Vector3.Zero;
            _p2.GlobalPosition = new Vector3(1.5f, 0.5f, -5f);
            _p2.Velocity = Vector3.Zero;
        }

        // P2 cammina oltre il bordo e resta appeso alla corda.
        if (_ticks > 30 && _ticks <= 150 && !_hungObserved)
            _p2.SetCommand(new PlayerCommand(new Vector2(0, -1f), false, false, false));

        if (_ticks > 60 && !_p2.IsOnFloor() && _p2.GlobalPosition.Y < 0f)
            _hungObserved = true;

        if (_ticks == 240)
        {
            Check(_hungObserved, "P2 è rimasto appeso alla corda nel vuoto");
            Check(_p2.GlobalPosition.Y > -8f,
                $"P2 non è caduto oltre il kill plane (Y {_p2.GlobalPosition.Y:F2})");
        }

        // Strategia da giocatore vero: P1 tiene la posizione d'ancora
        // contrastando il tiro; P2 riavvolge la corda (pendolo più corto e
        // alto del bordo), pompa il dondolio e salta da appeso in avanti.
        if (_ticks > 240 && !_landed)
        {
            float holdP1 = _p1.GlobalPosition.Z > -20.4f ? -1f : 0f;
            _p1.SetCommand(new PlayerCommand(new Vector2(0, holdP1), false, false, false));

            _jumpCooldown--;
            float vz = _p2.Velocity.Z;
            Vector3 pos = _p2.GlobalPosition;
            bool overPlatform = pos.Z < -18.7f && pos.Z > -26f && pos.Y > 0.7f;
            if (overPlatform)
            {
                // Sopra l'approdo: molla tutto e fatti calare dalla corda.
                _p2.SetCommand(new PlayerCommand(Vector2.Zero, false, false, false));
            }
            else
            {
                bool reel = _rope.CurrentLength > 3.2f;
                bool jump = !reel && vz < -2.5f && _jumpCooldown <= 0;
                if (jump)
                    _jumpCooldown = 90;
                float sign = Mathf.Abs(vz) < 0.1f ? -1f : Mathf.Sign(vz);
                _p2.SetCommand(new PlayerCommand(new Vector2(0, sign), jump, false, reel));
            }

            if (_p2.IsOnFloor() && _p2.GlobalPosition.Z < -18f && _p2.GlobalPosition.Y > 0.4f)
                _landed = true;
        }

        if (_ticks == 1200)
        {
            Check(_landed, $"P2 ha raggiunto l'approdo col dondolio ({_p2.GlobalPosition})");

            // Uscita: entrambi nella ExitZone (ultima stanza: banner finale).
            _p1.GlobalPosition = new Vector3(-1f, 1.5f, -37f);
            _p1.Velocity = Vector3.Zero;
            _p2.GlobalPosition = new Vector3(1f, 1.5f, -37f);
            _p2.Velocity = Vector3.Zero;
        }

        if (_ticks == 1260)
        {
            var banner = GetNode<Label>("Main/UI/Banner");
            Check(banner.Visible && banner.Text.Contains("completat"),
                $"uscita scattata con entrambi dentro (banner: \"{banner.Text}\")");

            GD.Print(_failed ? "ROOM04 TEST: FAIL" : "ROOM04 TEST: PASS");
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
