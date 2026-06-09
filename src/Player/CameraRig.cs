using Controluce.Core;
using Godot;

namespace Controluce.Player;

public partial class CameraRig : Node3D
{
    [Export] public Node3D? Target { get; set; }
    [Export] public float FollowSpeed { get; set; } = 6.0f;
    [Export] public Phase ViewPhase { get; set; } = Phase.Blue;

    public override void _Ready()
    {
        GetNode<Camera3D>("Camera3D").CullMask = PhaseLayers.CameraCullMaskFor(ViewPhase);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Target == null)
            return;

        float weight = 1f - Mathf.Exp(-FollowSpeed * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, weight);
    }
}
