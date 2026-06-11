using Godot;

namespace Controluce.Core;

// Barra HUD che mostra quanto è tesa la corda. Stile in tema "controluce":
// fondo scuro arrotondato, riempimento che scalda da ambra a rosso, e la
// barra si attenua quando la corda è lenta per non sporcare l'HUD.
public partial class TensionIndicator : ProgressBar
{
    private static readonly Color Calm = new(0.95f, 0.72f, 0.38f);
    private static readonly Color Strained = new(1f, 0.28f, 0.18f);

    [Export] public RopeConstraint? Constraint { get; set; }

    private StyleBoxFlat _fill = null!;

    public override void _Ready()
    {
        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.07f, 0.12f, 0.72f),
            BorderColor = new Color(0.4f, 0.25f, 0.16f, 0.6f),
            BorderWidthBottom = 2,
        };
        background.SetCornerRadiusAll(9);

        _fill = new StyleBoxFlat { BgColor = Calm };
        _fill.SetCornerRadiusAll(9);
        _fill.SetContentMarginAll(2f);

        AddThemeStyleboxOverride("background", background);
        AddThemeStyleboxOverride("fill", _fill);
    }

    public override void _Process(double delta)
    {
        if (Constraint == null)
            return;

        float tension = Constraint.Tension;
        Value = tension * 100f;
        _fill.BgColor = Calm.Lerp(Strained, Mathf.Clamp((tension - 0.5f) * 2f, 0f, 1f));

        Color faded = Modulate;
        faded.A = Mathf.Lerp(faded.A, tension > 0.05f ? 1f : 0.3f, (float)delta * 6f);
        Modulate = faded;
    }
}
