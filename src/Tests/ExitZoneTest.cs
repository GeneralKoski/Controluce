using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Verifica ExitZone: non scatta con un solo player dentro, scatta con entrambi.
public partial class ExitZoneTest : Node3D
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private bool _completed;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        _p1 = GetNode<PlayerController>("Player1");
        _p2 = GetNode<PlayerController>("Player2");
        GetNode<ExitZone>("ExitZone").RoomCompleted += () => _completed = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 30)
            _p1.GlobalPosition = new Vector3(10, 1, 0); // solo P1 nella zona

        if (_ticks == 90)
        {
            Check(!_completed, "non completata con un solo player");
            _p2.GlobalPosition = new Vector3(10, 1, 0);
        }

        if (_ticks == 150)
        {
            Check(_completed, "completata con entrambi i player");
            GD.Print(_failed ? "EXITZONE TEST: FAIL" : "EXITZONE TEST: PASS");
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
