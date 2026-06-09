using Godot;

namespace Controluce.Tests;

// Avvia main.tscn, salva uno screenshot per ciascun viewport e chiude.
// Uso: godot-mono --path . res://scenes/tests/test_screenshot.tscn
public partial class ScreenshotTest : Node
{
    private int _frames;

    public override void _Ready()
    {
        // CONTROLUCE_SHOT_SKINS="n,m" e CONTROLUCE_SHOT_SWAP=1 per fotografare
        // skin e scambio ruoli senza toccare user://settings.cfg.
        Core.Settings.Load();
        string[] skins = OS.GetEnvironment("CONTROLUCE_SHOT_SKINS").Split(',');
        if (skins.Length == 2 && int.TryParse(skins[0], out int s1) && int.TryParse(skins[1], out int s2))
        {
            Core.Settings.SkinP1 = s1;
            Core.Settings.SkinP2 = s2;
        }
        if (OS.GetEnvironment("CONTROLUCE_SHOT_SWAP") == "1")
            Core.Settings.SwapRoles = true;

        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
    }

    public override void _Process(double delta)
    {
        _frames++;

        // CONTROLUCE_SHOT_ROOM=n per fotografare una stanza diversa dalla prima.
        if (_frames == 5 && int.TryParse(OS.GetEnvironment("CONTROLUCE_SHOT_ROOM"), out int room) && room > 0)
            GetNode<Core.GameManager>("Main").LoadRoom(room);

        // CONTROLUCE_SHOT_SPREAD=1: porta P2 fuori dalla visuale di P1
        // (camera girata dall'altra parte) per vedere l'indicatore partner.
        if (_frames == 100 && OS.GetEnvironment("CONTROLUCE_SHOT_SPREAD") == "1")
        {
            var p1 = GetNode<Node3D>("Main/World/Player1");
            var p2 = GetNode<Node3D>("Main/World/Player2");
            p2.GlobalPosition = p1.GlobalPosition + new Vector3(0, -2f, -5f);
            p2.SetPhysicsProcess(false);
            GetNode<Node3D>("Main/World/Rope").SetPhysicsProcess(false);

            var rig = GetNode<Node3D>("Main/Split/ViewP1/ViewportP1/CameraRig");
            rig.SetPhysicsProcess(false);
            rig.Rotation = new Vector3(-0.3f, Mathf.Pi, 0);
        }

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
            camera.GlobalPosition = new Vector3(30, 25, -19);
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
