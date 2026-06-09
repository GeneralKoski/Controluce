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

        if (_frames == 150)
        {
            Save("Main/Split/ViewP1/ViewportP1", "/tmp/controluce_p1.png");
            Save("Main/Split/ViewP2/ViewportP2", "/tmp/controluce_p2.png");
            GetViewport().GetTexture().GetImage().SavePng("/tmp/controluce_full.png");

            // Vista dall'alto dell'intera stanza per il check della geometria.
            var rig = GetNode<Node3D>("Main/Split/ViewP1/ViewportP1/CameraRig");
            rig.SetPhysicsProcess(false);
            var camera = rig.GetNode<Camera3D>("Camera3D");
            camera.CullMask = uint.MaxValue;
            camera.GlobalPosition = new Vector3(26, 22, -19);
            camera.LookAt(new Vector3(0, 0, -19));
        }

        if (_frames == 160)
        {
            Save("Main/Split/ViewP1/ViewportP1", "/tmp/controluce_overview.png");
            GetTree().Quit();
        }
    }

    private void Save(string viewportPath, string filePath)
    {
        var viewport = GetNode<SubViewport>(viewportPath);
        viewport.GetTexture().GetImage().SavePng(filePath);
        GD.Print($"Screenshot salvato: {filePath}");
    }
}
