using Controluce.Core;
using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Verifica pedana a peso + piattaforma mobile + blocco a fase alternante.
public partial class MechanicsTest : Node3D
{
    private PlayerController _p1 = null!;
    private MovingPlatform _mover = null!;
    private PhaseBlock _toggle = null!;
    private Vector3 _moverStart;
    private Phase _initialPhase;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        _p1 = GetNode<PlayerController>("Player1");
        _mover = GetNode<MovingPlatform>("Mover");
        _toggle = GetNode<PhaseBlock>("Toggle");
        _moverStart = _mover.Position;
        _initialPhase = _toggle.BlockPhase;
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 30)
            _p1.GlobalPosition = new Vector3(5, 1, 0); // sale sulla pedana

        if (_ticks == 150)
        {
            Check(_mover.Position.DistanceTo(_moverStart) > 1f,
                $"piattaforma in moto con pedana premuta (spostata {_mover.Position.DistanceTo(_moverStart):F2} m)");
            _p1.GlobalPosition = new Vector3(-5, 1, 0); // scende dalla pedana
        }

        if (_ticks == 400)
        {
            Check(_mover.Position.DistanceTo(_moverStart) < 0.2f,
                $"piattaforma rientrata a riposo (distanza {_mover.Position.DistanceTo(_moverStart):F2} m)");
            Check(_toggle.BlockPhase != _initialPhase,
                $"blocco a tempo ha cambiato fase ({_initialPhase} -> {_toggle.BlockPhase})");

            GD.Print(_failed ? "MECHANICS TEST: FAIL" : "MECHANICS TEST: PASS");
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
