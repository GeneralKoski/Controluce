using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Stress test della corda: velocità estreme e teleport casuali (seed fisso).
// Verifica che dopo ogni tick il vincolo tenga (niente divergenza, niente NaN).
public partial class RopeTest : Node3D
{
    private const int TotalTicks = 600;
    private const float MarginOltreMax = 0.6f;

    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private RopeConstraint _rope = null!;
    private readonly RandomNumberGenerator _rng = new();
    private int _ticks;
    private bool _failed;
    private float _maxObserved;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100; // controlla DOPO che il vincolo ha lavorato
        _p1 = GetNode<PlayerController>("Player1");
        _p2 = GetNode<PlayerController>("Player2");
        _rope = GetNode<RopeConstraint>("Rope");
        _rng.Seed = 12345;
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        float distance = _p1.GlobalPosition.DistanceTo(_p2.GlobalPosition);
        _maxObserved = Mathf.Max(_maxObserved, distance);

        if (!float.IsFinite(distance))
        {
            GD.Print("FAIL - distanza NaN/inf al tick ", _ticks);
            _failed = true;
        }
        else if (distance > _rope.MaxLength + MarginOltreMax)
        {
            GD.Print($"FAIL - vincolo superato al tick {_ticks}: {distance:F2} > {_rope.MaxLength}");
            _failed = true;
        }

        if (_ticks >= TotalTicks || _failed)
        {
            GD.Print($"Distanza massima osservata: {_maxObserved:F2} (limite {_rope.MaxLength})");
            GD.Print(_failed ? "ROPE TEST: FAIL" : "ROPE TEST: PASS");
            GetTree().Quit(_failed ? 1 : 0);
            return;
        }

        // Movimenti bruschi: strattoni di velocità opposti e teleport occasionali.
        if (_ticks % 7 == 0)
        {
            _p1.Velocity = RandomVelocity(50f);
            _p2.Velocity = RandomVelocity(50f);
        }

        if (_ticks % 90 == 0)
        {
            _p1.GlobalPosition = _p2.GlobalPosition + RandomVelocity(1f).Normalized() * (_rope.MaxLength * 2f);
        }
    }

    private Vector3 RandomVelocity(float scale) => new(
        _rng.RandfRange(-scale, scale),
        _rng.RandfRange(-scale * 0.5f, scale),
        _rng.RandfRange(-scale, scale));
}
