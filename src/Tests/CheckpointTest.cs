using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Test integrato su main.tscn: attivazione checkpoint e respawn anti-rage.
public partial class CheckpointTest : Node
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _p1 = GetNode<PlayerController>("Main/World/Player1");
        _p2 = GetNode<PlayerController>("Main/World/Player2");
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 30)
        {
            // Porta entrambi sul checkpoint del mid platform.
            _p1.GlobalPosition = new Vector3(-1.5f, 1.5f, -16f);
            _p2.GlobalPosition = new Vector3(1.5f, 1.5f, -16f);
        }

        if (_ticks == 90)
        {
            // P1 cade nel vuoto.
            _p1.GlobalPosition = new Vector3(-1.5f, -20f, -10f);
        }

        if (_ticks == 150)
        {
            Vector3 expectedA = new(-1.5f, 1f, -16f);
            Vector3 expectedB = new(1.5f, 1f, -16f);
            Check(_p1.GlobalPosition.DistanceTo(expectedA) < 1.5f,
                $"P1 respawnato al checkpoint ({_p1.GlobalPosition})");
            Check(_p2.GlobalPosition.DistanceTo(expectedB) < 1.5f,
                $"P2 respawnato al checkpoint ({_p2.GlobalPosition})");

            GD.Print(_failed ? "CHECKPOINT TEST: FAIL" : "CHECKPOINT TEST: PASS");
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
