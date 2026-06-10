using Controluce.Core;
using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Test integrato della stanza 5 "La porta sul vuoto": P2 sta sulla pedana
// in fondo al ponte (ancora + porta giù), P1 attraversa appeso con
// riavvolgimento e dondolio; l'uscita scatta con entrambi dentro.
public partial class Room05Test : Node
{
    private GameManager _gm = null!;
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private MovingPlatform _door = null!;
    private RopeConstraint _rope = null!;
    private int _ticks;
    private bool _failed;
    private bool _hungObserved;
    private bool _landed;
    private int _jumpCooldown;

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
        {
            _gm.LoadRoom(4);
            _door = GetNode<MovingPlatform>("Main/World/RoomSlot/Room05/Porta");
        }

        if (_ticks == 30)
        {
            Check(_door.GlobalPosition.Y > 0.5f, "porta alzata finché la pedana è libera");

            // P2 in fondo al ponte, sopra la pedana; P1 al bordo del vuoto.
            _p2.GlobalPosition = new Vector3(1.5f, 4.5f, -21f);
            _p2.Velocity = Vector3.Zero;
            _p1.GlobalPosition = new Vector3(-1.5f, 0.5f, -5f);
            _p1.Velocity = Vector3.Zero;
        }

        if (_ticks == 150)
            Check(_door.GlobalPosition.Y < -2f,
                $"P2 sulla pedana abbassa la porta (Y {_door.GlobalPosition.Y:F2})");

        // P2 tiene la posizione sulla pedana contrastando il tiro della corda.
        if (_ticks > 30)
        {
            Vector2 hold = new(
                Mathf.Clamp(1.5f - _p2.GlobalPosition.X, -1f, 1f),
                Mathf.Clamp(-21f - _p2.GlobalPosition.Z, -1f, 1f));
            _p2.SetCommand(new PlayerCommand(hold.LimitLength(1f), false, false, false));
        }

        // P1 cammina oltre il bordo e resta appeso.
        if (_ticks > 30 && _ticks <= 180 && !_hungObserved)
            _p1.SetCommand(new PlayerCommand(new Vector2(0, -1f), false, false, false));

        if (_ticks > 60 && !_p1.IsOnFloor() && _p1.GlobalPosition.Y < 0f)
            _hungObserved = true;

        if (_ticks == 240)
        {
            Check(_hungObserved, "P1 è rimasto appeso alla corda nel vuoto");
            Check(_p1.GlobalPosition.Y > -8f,
                $"P1 non è caduto oltre il kill plane (Y {_p1.GlobalPosition.Y:F2})");
        }

        // P1 riavvolge, pompa e salta (P2 resta in tenuta sulla pedana).
        if (_ticks > 240 && !_landed)
        {
            _jumpCooldown--;
            float vz = _p1.Velocity.Z;
            Vector3 pos = _p1.GlobalPosition;
            bool overPlatform = pos.Z < -20.2f && pos.Z > -26.8f && pos.Y > 0.7f;
            if (overPlatform)
            {
                _p1.SetCommand(new PlayerCommand(Vector2.Zero, false, false, false));
            }
            else
            {
                bool reel = _rope.CurrentLength > 3.2f;
                bool jump = !reel && vz < -2.5f && _jumpCooldown <= 0;
                if (jump)
                    _jumpCooldown = 90;
                float sign = Mathf.Abs(vz) < 0.1f ? -1f : Mathf.Sign(vz);
                _p1.SetCommand(new PlayerCommand(new Vector2(0, sign), jump, false, reel));
            }

            if (_p1.IsOnFloor() && pos.Z < -19.5f && pos.Y > 0.4f)
                _landed = true;
        }

        if (_ticks == 1200)
        {
            Check(_landed, $"P1 ha superato la porta col dondolio ({_p1.GlobalPosition})");

            _p1.GlobalPosition = new Vector3(-1f, 1.5f, -37.5f);
            _p1.Velocity = Vector3.Zero;
            _p2.GlobalPosition = new Vector3(1f, 1.5f, -37.5f);
            _p2.Velocity = Vector3.Zero;
        }

        if (_ticks == 1260)
        {
            var banner = GetNode<Label>("Main/UI/Banner");
            Check(banner.Visible && banner.Text.Contains("completat"),
                $"uscita scattata con entrambi dentro (banner: \"{banner.Text}\")");

            GD.Print(_failed ? "ROOM05 TEST: FAIL" : "ROOM05 TEST: PASS");
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
