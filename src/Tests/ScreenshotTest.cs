using Godot;

namespace Controluce.Tests;

// Avvia main.tscn, salva uno screenshot per ciascun viewport e chiude.
// Uso: godot-mono --path . res://scenes/tests/test_screenshot.tscn
public partial class ScreenshotTest : Node
{
    private int _frames;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (_frames < 30)
            return;

        Save("Main/Split/ViewP1/ViewportP1", "/tmp/controluce_p1.png");
        Save("Main/Split/ViewP2/ViewportP2", "/tmp/controluce_p2.png");
        GetTree().Quit();
    }

    private void Save(string viewportPath, string filePath)
    {
        var viewport = GetNode<SubViewport>(viewportPath);
        viewport.GetTexture().GetImage().SavePng(filePath);
        GD.Print($"Screenshot salvato: {filePath}");
    }
}
