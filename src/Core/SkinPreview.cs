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
        viewport.AddChild(_pivot);

        _body = new MeshInstance3D
        {
            Mesh = new CapsuleMesh(),
            Position = new Vector3(0f, 1f, 0f),
        };
        _pivot.AddChild(_body);
    }

    public override void _Process(double delta)
    {
        _pivot.Rotation = new Vector3(0f, _pivot.Rotation.Y + (float)delta * 0.8f, 0f);
    }

    public void ShowSkin(Phase phase, int skin) => PlayerSkin.Apply(_body, phase, skin);
}
