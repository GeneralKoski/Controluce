using Godot;

namespace Controluce.Core;

// Schermata di fine gioco: compare dopo l'ultima stanza con una dissolvenza
// scura, il titolo e il ritorno al menu. Costruita in codice, in tema.
public partial class FinaleScreen : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Theme = GD.Load<Theme>("res://assets/ui_theme.tres");
        Modulate = new Color(1f, 1f, 1f, 0f);

        var backdrop = new ColorRect { Color = new Color(0.02f, 0.02f, 0.05f, 0.88f) };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var box = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -360,
            OffsetRight = 360,
            OffsetTop = -180,
            OffsetBottom = 180,
        };
        box.AddThemeConstantOverride("separation", 24);
        AddChild(box);

        var title = new Label
        {
            Text = "Hai completato Controluce",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 56);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.6f));
        title.AddThemeColorOverride("font_shadow_color", new Color(0.6f, 0.25f, 0.1f, 0.6f));
        title.AddThemeConstantOverride("shadow_offset_y", 3);
        box.AddChild(title);

        var subtitle = new Label
        {
            Text = "Due percezioni, una corda. Nessuno ce l'ha fatta da solo.",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeFontSizeOverride("font_size", 26);
        subtitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.82f, 0.9f));
        box.AddChild(subtitle);

        var spacer = new Control { CustomMinimumSize = new Vector2(0, 16) };
        box.AddChild(spacer);

        var menuButton = new Button { Text = "Torna al menu" };
        menuButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        menuButton.Pressed += ReturnToMenu;
        box.AddChild(menuButton);

        Input.MouseMode = Input.MouseModeEnum.Visible;
        menuButton.GrabFocus();

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1f, 1.2f);
    }

    private void ReturnToMenu()
    {
        GameConfig.Mode = null;
        Multiplayer.MultiplayerPeer = null;
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
    }
}
