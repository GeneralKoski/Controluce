using Godot;

namespace Controluce.Player;

public partial class PlayerInput : Node
{
    [Export] public string Prefix { get; set; } = "p1";

    public Vector2 GetMoveAxis() =>
        Input.GetVector($"{Prefix}_left", $"{Prefix}_right", $"{Prefix}_forward", $"{Prefix}_back");

    public bool IsJumpJustPressed() => Input.IsActionJustPressed($"{Prefix}_jump");

    public bool IsJumpHeld() => Input.IsActionPressed($"{Prefix}_jump");

    public bool IsPingJustPressed() => Input.IsActionJustPressed($"{Prefix}_ping");
}
