using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Verifica contrappeso: P2 resta a terra su una piattaforma alta, P1 viene
// spinto oltre il bordo. La corda deve sostenerlo (pendolo), non farlo cadere.
public partial class CounterweightTest : Node3D
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private RopeConstraint _rope = null!;
    private int _ticks;
    private float _minP1Y = float.MaxValue;
    private bool _failed;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        _p1 = GetNode<PlayerController>("Player1");
        _p2 = GetNode<PlayerController>("Player2");
        _rope = GetNode<RopeConstraint>("Rope");
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks >= 30 && _ticks < 60)
            _p1.Velocity = new Vector3(10f, _p1.Velocity.Y, 0f); // spinta oltre il bordo

        if (_ticks > 30)
            _minP1Y = Mathf.Min(_minP1Y, _p1.GlobalPosition.Y);

        if (_ticks == 360)
        {
            float platformY = 8f;
            Check(_minP1Y > platformY - _rope.MaxLength - 1.5f,
                $"P1 non cade nel vuoto (minY {_minP1Y:F2})");
            Check(!_p1.IsOnFloor() && _p1.GlobalPosition.Y < platformY,
                "P1 è rimasto appeso sotto la piattaforma");
            Check(_p2.IsOnFloor(), "P2 (ancora) è rimasto sulla piattaforma");
            Check(_rope.Tension > 0.9f, $"corda tesa (tensione {_rope.Tension:F2})");

            GD.Print(_failed ? "COUNTERWEIGHT TEST: FAIL" : "COUNTERWEIGHT TEST: PASS");
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
