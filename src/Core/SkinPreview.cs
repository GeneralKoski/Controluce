using Controluce.Player;
using Godot;

namespace Controluce.Core;

// Anteprima 3D di una skin nel menu Personaggi: un piccolo SubViewport con
// mondo proprio, la capsula del player con la skin applicata e una rotazione
// lenta. Tutto costruito in codice, nessun asset.
public partial class SkinPreview : SubViewportContainer
{
    private MeshInstance3D _body = null!;
    private Node3D _pivot = null!;
    private float _time;

    public override void _Ready()
    {
        Stretch = true;
        CustomMinimumSize = new Vector2(220, 260);

        var viewport = new SubViewport
        {
            OwnWorld3D = true,
            World3D = new World3D(),
            TransparentBg = true,
            Msaa3D = Viewport.Msaa.Msaa4X,
        };
        AddChild(viewport);

        var camera = new Camera3D { Fov = 38f };
        camera.Position = new Vector3(0f, 1.6f, 4.4f);
        camera.LookAtFromPosition(camera.Position, new Vector3(0f, 1.35f, 0f), Vector3.Up);
        viewport.AddChild(camera);

        var sun = new DirectionalLight3D
        {
            LightColor = new Color(1f, 0.82f, 0.6f),
            LightEnergy = 1.6f,
        };
        sun.RotationDegrees = new Vector3(-30f, 35f, 0f);
        viewport.AddChild(sun);

        var fill = new DirectionalLight3D
        {
            LightColor = new Color(0.45f, 0.55f, 0.85f),
            LightEnergy = 0.7f,
        };
        fill.RotationDegrees = new Vector3(-15f, -140f, 0f);
        viewport.AddChild(fill);

        _pivot = new Node3D();
        // Parte rivolto verso la camera, poi gira su sé stesso.
        _pivot.Rotation = new Vector3(0f, Mathf.Pi, 0f);
        viewport.AddChild(_pivot);

        var pedestal = new MeshInstance3D
        {
            Mesh = new CylinderMesh
            {
                TopRadius = 0.85f,
                BottomRadius = 0.95f,
                Height = 0.12f,
                RadialSegments = 32,
            },
            Position = new Vector3(0f, -0.06f, 0f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.10f, 0.10f, 0.18f),
                Roughness = 0.7f,
                EmissionEnabled = true,
                Emission = new Color(0.45f, 0.28f, 0.14f) * 0.25f,
            },
        };
        viewport.AddChild(pedestal);

        _body = new MeshInstance3D
        {
            Mesh = new CapsuleMesh(),
            Position = new Vector3(0f, 1f, 0f),
        };
        _pivot.AddChild(_body);
        BuildEyes();
    }

    // Gli stessi occhi del player in gioco (PlayerVisual), per riconoscersi.
    private void BuildEyes()
    {
        var white = new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        var black = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.08f, 0.08f, 0.1f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        foreach (float side in new[] { -1f, 1f })
        {
            _body.AddChild(new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.09f, Height = 0.18f, RadialSegments = 16, Rings = 8 },
                MaterialOverride = white,
                Position = new Vector3(side * 0.17f, 0.32f, -0.42f),
            });
            _body.AddChild(new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f, RadialSegments = 16, Rings = 8 },
                MaterialOverride = black,
                Position = new Vector3(side * 0.16f, 0.32f, -0.49f),
            });
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _pivot.Rotation = new Vector3(0f, _pivot.Rotation.Y + (float)delta * 0.8f, 0f);
        _body.Position = new Vector3(0f, 1f + Mathf.Sin(_time * 1.6f) * 0.04f, 0f);
    }

    public void ShowSkin(Phase phase, int skin) => PlayerSkin.Apply(_body, phase, skin);
}
