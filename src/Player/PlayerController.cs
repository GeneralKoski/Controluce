using Controluce.Core;
using Godot;

namespace Controluce.Player;

// Simulazione del player: consuma solo PlayerCommand, non legge mai
// i dispositivi di input direttamente.
public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void PingedEventHandler(Vector3 position, int phase);

    [Export] public Phase PlayerPhase { get; set; } = Phase.Blue;
    [Export] public float Speed { get; set; } = 6.0f;
    [Export] public float Acceleration { get; set; } = 40.0f;
    [Export] public float JumpVelocity { get; set; } = 9.0f;
    [Export] public float Gravity { get; set; } = 24.0f;
    // Accelerazione di "pompata" del dondolio da appesi (m/s^2).
    [Export] public float SwingPump { get; set; } = 10.0f;

    private PlayerCommand _command;
    private AudioStreamPlayer3D _stepAudio = null!;
    private float _stepTimer;
    private bool _hanging;
    private Vector3 _ropeDir;

    // Letto da RopeConstraint (che fa lo step dopo i player).
    public bool IsPullingRope { get; private set; }

    public void SetCommand(PlayerCommand command) => _command = command;

    // Impostato da RopeConstraint quando il player penzola dalla corda.
    public void SetRopeHang(bool hanging, Vector3 ropeDir)
    {
        _hanging = hanging;
        _ropeDir = ropeDir;
    }

    public override void _Ready()
    {
        CollisionLayer = PhaseLayers.Player;
        CollisionMask = PhaseLayers.PlayerCollisionMaskFor(PlayerPhase);

        _stepAudio = new AudioStreamPlayer3D
        {
            Stream = AudioSynth.NoiseBurst(0.09f),
            VolumeDb = -10f,
        };
        AddChild(_stepAudio);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= Gravity * dt;

        if (_command.JumpPressed && IsOnFloor())
            velocity.Y = JumpVelocity;

        if (_hanging && !IsOnFloor())
        {
            // Da appesi niente freno aereo: l'input pompa il dondolio
            // sul piano tangente alla corda, il pendolo fa il resto.
            Vector3 horizontalSpeed = velocity with { Y = 0 };
            if (horizontalSpeed.Length() < Speed * 1.6f)
            {
                Vector3 pump = new Vector3(_command.MoveAxis.X, 0, _command.MoveAxis.Y) * SwingPump;
                pump -= _ropeDir * pump.Dot(_ropeDir);
                velocity += pump * dt;
            }
        }
        else
        {
            Vector3 target = new Vector3(_command.MoveAxis.X, 0, _command.MoveAxis.Y) * Speed;
            velocity.X = Mathf.MoveToward(velocity.X, target.X, Acceleration * dt);
            velocity.Z = Mathf.MoveToward(velocity.Z, target.Z, Acceleration * dt);
        }

        Velocity = velocity;
        MoveAndSlide();

        UpdateFootsteps(dt);

        if (_command.PingPressed)
            SpawnPing();

        IsPullingRope = _command.PullHeld;
        _command = default;
    }

    private void UpdateFootsteps(float dt)
    {
        Vector3 horizontal = Velocity with { Y = 0 };
        if (!IsOnFloor() || horizontal.Length() < 1f)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= dt;
        if (_stepTimer > 0f)
            return;

        _stepTimer = 2.2f / horizontal.Length();
        _stepAudio.PitchScale = 0.9f + GD.Randf() * 0.2f;
        _stepAudio.Play();
    }

    private void SpawnPing()
    {
        var marker = new PingMarker
        {
            Color = PlayerPhase == Phase.Blue ? new Color(0.4f, 0.65f, 1f) : new Color(1f, 0.45f, 0.4f),
        };
        GetParent().AddChild(marker);
        marker.GlobalPosition = GlobalPosition + Vector3.Up * 2.5f;
        EmitSignal(SignalName.Pinged, marker.GlobalPosition, (int)PlayerPhase);
    }
}
