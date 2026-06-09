using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Verifica dondolio: da appeso, pompando con l'input l'oscillazione cresce;
// senza input il pendolo continua a oscillare (smorzamento leggero).
public partial class SwingTest : Node3D
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private int _ticks;
    private bool _failed;
    private float _maxPumpSpeed;
    private float _maxResidualSpeed;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 100;
        _p1 = GetNode<PlayerController>("Player1");
        _p2 = GetNode<PlayerController>("Player2");

        _p1.GetNode("PlayerInput").QueueFree();
        _p2.GetNode("PlayerInput").QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 30)
        {
            _p1.GlobalPosition = new Vector3(1f, 4f, 0f);
            _p1.Velocity = Vector3.Zero;
        }

        if (_ticks == 240)
            Check(!_p1.IsOnFloor() && _p1.GlobalPosition.Y < 5f,
                $"P1 penzola fermo (Y {_p1.GlobalPosition.Y:F2}, velX {_p1.Velocity.X:F2})");

        if (_ticks > 240 && _ticks <= 540)
        {
            // Pompata risonante: spinge nel verso del moto, come su un'altalena.
            float sign = Mathf.Abs(_p1.Velocity.X) < 0.1f ? 1f : Mathf.Sign(_p1.Velocity.X);
            _p1.SetCommand(new PlayerCommand(new Vector2(sign, 0f), false, false, false));
            _maxPumpSpeed = Mathf.Max(_maxPumpSpeed, Mathf.Abs(_p1.Velocity.X));
        }

        if (_ticks == 540)
            Check(_maxPumpSpeed > 3f, $"il dondolio cresce pompando (picco {_maxPumpSpeed:F2} m/s)");

        if (_ticks > 780)
            _maxResidualSpeed = Mathf.Max(_maxResidualSpeed, Mathf.Abs(_p1.Velocity.X));

        if (_ticks == 900)
        {
            Check(_maxResidualSpeed > 1.5f,
                $"il dondolio persiste senza input (picco {_maxResidualSpeed:F2} m/s dopo 4s)");

            // Salto da appeso: sgancia lo slancio verso l'alto.
            _p1.SetCommand(new PlayerCommand(Vector2.Zero, true, false, false));
        }

        if (_ticks == 905)
        {
            Check(_p1.Velocity.Y > 5f, $"salto da appeso eseguito (velY {_p1.Velocity.Y:F2})");

            GD.Print(_failed ? "SWING TEST: FAIL" : "SWING TEST: PASS");
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
