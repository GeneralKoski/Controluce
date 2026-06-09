using Controluce.Core;
using Godot;

namespace Controluce.Level;

// Piattaforma mobile con fase. Due usi:
// - Autonomous: fa avanti-indietro da sola tra origine e origine+Offset;
// - attivata (IActivatable): va verso Offset quando attiva, torna quando inattiva.
//   Con Offset verticale negativo funziona da porta che sprofonda nel pavimento.
[Tool]
public partial class MovingPlatform : AnimatableBody3D, IActivatable
{
    private Phase _phase = Phase.Neutral;
    private Vector3 _size = new(3f, 0.5f, 3f);
    private Vector3 _origin;
    private float _progress;
    private int _direction = 1;
    private float _pause;
    private bool _active;

    [Export] public Vector3 Offset { get; set; } = new(0, 0, -5);
    [Export] public float Speed { get; set; } = 2.5f;
    [Export] public bool Autonomous { get; set; }
    [Export] public float PauseAtEnds { get; set; } = 0.6f;

    [Export]
    public Phase BlockPhase
    {
        get => _phase;
        set { _phase = value; if (IsInsideTree()) Rebuild(); }
    }

    [Export]
    public Vector3 Size
    {
        get => _size;
        set { _size = value; if (IsInsideTree()) Rebuild(); }
    }

    public override void _Ready()
    {
        Rebuild();
        if (!Engine.IsEditorHint())
        {
            _origin = Position;
            SyncToPhysics = true;
        }
    }

    public void SetActivated(bool active) => _active = active;

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || Offset.LengthSquared() < 1e-6f)
            return;

        float dt = (float)delta;
        float rate = Speed / Offset.Length();

        if (Autonomous)
        {
            if (_pause > 0f)
            {
                _pause -= dt;
                return;
            }

            _progress += _direction * rate * dt;
            if (_progress >= 1f || _progress <= 0f)
            {
                _progress = Mathf.Clamp(_progress, 0f, 1f);
                _direction = -_direction;
                _pause = PauseAtEnds;
            }
        }
        else
        {
            _progress = Mathf.MoveToward(_progress, _active ? 1f : 0f, rate * dt);
        }

        Position = _origin + Offset * _progress;
    }

    private void Rebuild() => PhaseGeometry.Build(this, _phase, _size);
}
