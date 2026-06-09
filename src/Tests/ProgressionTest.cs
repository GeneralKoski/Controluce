using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Verifica progressione: completare la stanza 1 carica la stanza 2
// e respawna i player ai suoi spawn.
public partial class ProgressionTest : Node
{
    private GameManager _gm = null!;
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _gm = GetNode<GameManager>("Main");
        _p1 = GetNode<PlayerController>("Main/World/Player1");
        _p2 = GetNode<PlayerController>("Main/World/Player2");
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 30)
        {
            Check(_gm.CurrentRoomIndex == 0, "parte dalla stanza 1");
            // Entrambi nell'uscita della stanza 1.
            _p1.GlobalPosition = new Vector3(-1f, 1.5f, -36f);
            _p2.GlobalPosition = new Vector3(1f, 1.5f, -36f);
        }

        if (_ticks == 300) // dopo banner (1.2s) + fade (0.6+0.6s)
        {
            Check(_gm.CurrentRoomIndex == 1, $"caricata la stanza 2 (indice {_gm.CurrentRoomIndex})");
            Check(_p1.GlobalPosition.DistanceTo(new Vector3(-1.5f, 1f, -2f)) < 2f,
                $"P1 allo spawn della stanza 2 ({_p1.GlobalPosition})");
            Check(_p2.GlobalPosition.DistanceTo(new Vector3(1.5f, 1f, -2f)) < 2f,
                $"P2 allo spawn della stanza 2 ({_p2.GlobalPosition})");

            GD.Print(_failed ? "PROGRESSION TEST: FAIL" : "PROGRESSION TEST: PASS");
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
