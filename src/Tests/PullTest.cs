using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Verifica tira-corda: P1 penzola nel vuoto appeso a P2 (a terra);
// P2 tiene premuto "pull" e P1 deve risalire fin quasi al suo livello.
public partial class PullTest : Node3D
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private RopeConstraint _rope = null!;
    private float _hangY;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        _p1 = GetNode<PlayerController>("Player1");
        _p2 = GetNode<PlayerController>("Player2");
        _rope = GetNode<RopeConstraint>("Rope");

        // I comandi li inietta il test, non l'input locale.
        _p1.GetNode("PlayerInput").QueueFree();
        _p2.GetNode("PlayerInput").QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 30)
        {
            // P1 caduto nel "vuoto rosso": parte già a penzoloni sotto P2.
            _p1.GlobalPosition = new Vector3(1f, 4f, 0f);
            _p1.Velocity = Vector3.Zero;
        }

        if (_ticks == 240)
        {
            _hangY = _p1.GlobalPosition.Y;
            Check(!_p1.IsOnFloor() && _hangY < 5f, $"P1 penzola nel vuoto (Y {_hangY:F2})");
        }

        if (_ticks > 240)
            _p2.SetCommand(new PlayerCommand(Vector2.Zero, false, false, true)); // P2 tira

        if (_ticks == 600)
        {
            float risen = _p1.GlobalPosition.Y - _hangY;
            Check(_rope.CurrentLength <= _rope.MinLength + 0.1f,
                $"corda riavvolta ({_rope.CurrentLength:F2} m)");
            Check(risen > 3f, $"P1 risalito di {risen:F2} m (Y {_p1.GlobalPosition.Y:F2})");
            Check(_p2.IsOnFloor(), "P2 è rimasto sulla piattaforma");

            GD.Print(_failed ? "PULL TEST: FAIL" : "PULL TEST: PASS");
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
