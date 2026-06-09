using Godot;

namespace Controluce.Core;

// Barra HUD che mostra quanto è tesa la corda.
public partial class TensionIndicator : ProgressBar
{
    private static readonly Color Calm = new(0.4f, 0.9f, 0.5f);
    private static readonly Color Strained = new(1f, 0.3f, 0.2f);

    [Export] public RopeConstraint? Constraint { get; set; }

    public override void _Process(double delta)
    {
        if (Constraint == null)
            return;

        Value = Constraint.Tension * 100f;
        Modulate = Calm.Lerp(Strained, Mathf.Clamp((Constraint.Tension - 0.5f) * 2f, 0f, 1f));
    }
}
