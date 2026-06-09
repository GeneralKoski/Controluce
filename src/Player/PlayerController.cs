using Godot;

namespace Controluce.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed { get; set; } = 6.0f;
    [Export] public float Acceleration { get; set; } = 40.0f;
    [Export] public float JumpVelocity { get; set; } = 9.0f;
    [Export] public float Gravity { get; set; } = 24.0f;

    private PlayerInput _input = null!;

    public override void _Ready()
    {
        _input = GetNode<PlayerInput>("PlayerInput");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= Gravity * dt;

        if (_input.IsJumpJustPressed() && IsOnFloor())
            velocity.Y = JumpVelocity;

        Vector2 axis = _input.GetMoveAxis();
        Vector3 target = new Vector3(axis.X, 0, axis.Y) * Speed;
        velocity.X = Mathf.MoveToward(velocity.X, target.X, Acceleration * dt);
        velocity.Z = Mathf.MoveToward(velocity.Z, target.Z, Acceleration * dt);

        Velocity = velocity;
        MoveAndSlide();
    }
}
