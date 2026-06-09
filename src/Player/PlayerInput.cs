using Controluce.Core;
using Godot;

namespace Controluce.Player;

// Layer di input locale: legge i dispositivi e alimenta la simulazione
// con PlayerCommand. Nessuna logica di gioco qui.
public partial class PlayerInput : Node
{
    [Export] public string Prefix { get; set; } = "p1";

    private PlayerController _controller = null!;

    public override void _Ready()
    {
        _controller = GetParent<PlayerController>();
        // I comandi vanno consegnati prima che la simulazione faccia lo step.
        ProcessPhysicsPriority = -10;
    }

    public override void _PhysicsProcess(double delta)
    {
        _controller.SetCommand(Capture());
    }

    public PlayerCommand Capture() => CaptureFrom(Prefix);

    public static PlayerCommand CaptureFrom(string prefix) => new(
        Input.GetVector($"{prefix}_left", $"{prefix}_right", $"{prefix}_forward", $"{prefix}_back"),
        Input.IsActionJustPressed($"{prefix}_jump"),
        Input.IsActionJustPressed($"{prefix}_ping"),
        Input.IsActionPressed($"{prefix}_pull"));
}
