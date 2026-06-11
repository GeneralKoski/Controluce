using Godot;

namespace Controluce.Tests;

// Istanzia il menu e salva uno screenshot della finestra.
public partial class MenuScreenshotTest : Node
{
    private int _frames;

    public override void _Ready()
    {
        // CONTROLUCE_SHOT_SKINS="n,m" per fotografare skin diverse.
        string skins = OS.GetEnvironment("CONTROLUCE_SHOT_SKINS");
        if (skins.Length > 0)
        {
            string[] parts = skins.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int s1) && int.TryParse(parts[1], out int s2))
            {
                Core.Settings.Load();
                Core.Settings.SkinP1 = s1;
                Core.Settings.SkinP2 = s2;
            }
        }

        var menu = GD.Load<PackedScene>("res://scenes/menu.tscn").Instantiate();
        AddChild(menu);

        // CONTROLUCE_SHOT_PANEL=Online|Options|Skins per fotografare un pannello.
        string panel = OS.GetEnvironment("CONTROLUCE_SHOT_PANEL");
        if (panel.Length > 0 && menu.GetNodeOrNull<Control>(panel) is { } target)
        {
            menu.GetNode<Control>("Home").Visible = false;
            target.Visible = true;
        }
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (_frames < 20)
            return;

        GetViewport().GetTexture().GetImage().SavePng("/tmp/controluce_menu.png");
        GD.Print("Screenshot salvato: /tmp/controluce_menu.png");
        GetTree().Quit();
    }
}
