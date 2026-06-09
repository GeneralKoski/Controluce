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
    private Camera3D _camera = null!;
    private Vector3 _cameraHome;

    public float Yaw => _yaw;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _camera.CullMask = PhaseLayers.CameraCullMaskFor(ViewPhase);
        _cameraHome = _camera.Position;
        _yaw = Rotation.Y;
        _pitch = Rotation.X;

        Settings.Load();
        MouseSensitivity = Settings.MouseSensitivity;
        StickSpeed = Settings.StickSpeed;

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

        AvoidOcclusion();
    }

    // La camera non entra nella geometria che questo player vede solida:
    // se il raggio pivot->camera colpisce qualcosa, la avvicina.
    private void AvoidOcclusion()
    {
        Vector3 pivot = GlobalPosition + Vector3.Up * 1.2f;
        Vector3 desired = GlobalTransform * _cameraHome;
        uint mask = PhaseLayers.Neutral | PhaseLayers.GeometryLayerFor(ViewPhase);

        var query = PhysicsRayQueryParameters3D.Create(pivot, desired, mask);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (hit.Count > 0)
        {
            Vector3 direction = (desired - pivot).Normalized();
            _camera.GlobalPosition = (Vector3)hit["position"] - direction * 0.35f;
        }
        else
        {
            _camera.Position = _cameraHome;
        }
    }

    private static float ApplyDeadzone(float value) =>
        Mathf.Abs(value) < 0.15f ? 0f : value;
}
