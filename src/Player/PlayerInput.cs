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

    public PlayerCommand Capture() => new(
        Input.GetVector($"{Prefix}_left", $"{Prefix}_right", $"{Prefix}_forward", $"{Prefix}_back"),
        Input.IsActionJustPressed($"{Prefix}_jump"),
        Input.IsActionJustPressed($"{Prefix}_ping"));
}
