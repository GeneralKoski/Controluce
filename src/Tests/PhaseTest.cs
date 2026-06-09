using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Scena di test headless: verifica che ogni player collida solo con la propria fase.
// P1 (blu) parte sopra un blocco rosso e deve cadere; P2 (rosso) idem sul blu.
// Poi vengono spostati sopra la propria fase e devono restare in piedi.
public partial class PhaseTest : Node3D
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        _p1 = GetNode<PlayerController>("Player1");
        _p2 = GetNode<PlayerController>("Player2");
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 90)
        {
            Check(_p1.GlobalPosition.Y < -2f, "P1 (blu) cade attraverso il blocco rosso");
            Check(_p2.GlobalPosition.Y < -2f, "P2 (rosso) cade attraverso il blocco blu");

            _p1.GlobalPosition = new Vector3(-4, 4, 0);
            _p1.Velocity = Vector3.Zero;
            _p2.GlobalPosition = new Vector3(4, 4, 0);
            _p2.Velocity = Vector3.Zero;
        }

        if (_ticks == 180)
        {
            Check(_p1.IsOnFloor() && _p1.GlobalPosition.Y > 0f, "P1 (blu) sta in piedi sul blocco blu");
            Check(_p2.IsOnFloor() && _p2.GlobalPosition.Y > 0f, "P2 (rosso) sta in piedi sul blocco rosso");

            GD.Print(_failed ? "PHASE TEST: FAIL" : "PHASE TEST: PASS");
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
