using Godot;

namespace Controluce.Tests;

// Istanzia il menu e salva uno screenshot della finestra.
public partial class MenuScreenshotTest : Node
{
    private int _frames;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/menu.tscn").Instantiate());
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
