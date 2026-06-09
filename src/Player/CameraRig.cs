using Controluce.Core;
using Godot;

namespace Controluce.Player;

public partial class CameraRig : Node3D
{
    [Export] public Node3D? Target { get; set; }
    [Export] public float FollowSpeed { get; set; } = 6.0f;
    [Export] public Phase ViewPhase { get; set; } = Phase.Blue;

    // Rotazione orbitale: mouse (P1) e/o stick destro del gamepad (P2).
    [Export] public bool UseMouse { get; set; }
    [Export] public bool UseStick { get; set; }
    [Export] public float MouseSensitivity { get; set; } = 0.003f;
    [Export] public float StickSpeed { get; set; } = 2.5f;
    [Export] public float MinPitch { get; set; } = -0.5f;
    [Export] public float MaxPitch { get; set; } = 0.35f;

    private float _yaw;
    private float _pitch;

    public float Yaw => _yaw;

    public override void _Ready()
    {
        GetNode<Camera3D>("Camera3D").CullMask = PhaseLayers.CameraCullMaskFor(ViewPhase);
        _yaw = Rotation.Y;
        _pitch = Rotation.X;

        if (UseMouse)
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!UseMouse || @event is not InputEventMouseMotion motion
            || Input.MouseMode != Input.MouseModeEnum.Captured)
            return;

        _yaw -= motion.Relative.X * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (UseStick)
        {
            float x = ApplyDeadzone(Input.GetJoyAxis(0, JoyAxis.RightX));
            float y = ApplyDeadzone(Input.GetJoyAxis(0, JoyAxis.RightY));
            _yaw -= x * StickSpeed * dt;
            _pitch = Mathf.Clamp(_pitch - y * StickSpeed * 0.6f * dt, MinPitch, MaxPitch);
        }

        Rotation = new Vector3(_pitch, _yaw, 0);

        if (Target == null)
            return;

        float weight = 1f - Mathf.Exp(-FollowSpeed * dt);
        GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, weight);
    }

    private static float ApplyDeadzone(float value) =>
        Mathf.Abs(value) < 0.15f ? 0f : value;
}
