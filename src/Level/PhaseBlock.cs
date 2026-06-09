using Controluce.Core;
using Godot;

namespace Controluce.Level;

[Tool]
public partial class PhaseBlock : StaticBody3D
{
    private static readonly Color BlueColor = new(0.25f, 0.5f, 1f);
    private static readonly Color RedColor = new(1f, 0.3f, 0.25f);

    private Phase _phase = Phase.Blue;
    private Vector3 _size = new(2f, 0.5f, 2f);

    [Export]
    public Phase BlockPhase
    {
        get => _phase;
        set { _phase = value; if (IsInsideTree()) Rebuild(); }
    }

    [Export]
    public Vector3 Size
    {
        get => _size;
        set { _size = value; if (IsInsideTree()) Rebuild(); }
    }

    public override void _Ready() => Rebuild();

    private void Rebuild()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        CollisionLayer = PhaseLayers.GeometryLayerFor(_phase);
        CollisionMask = 0;

        var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = _size } };
        AddChild(shape);

        Color color = _phase == Phase.Blue ? BlueColor : RedColor;
        var mesh = new BoxMesh { Size = _size };

        var solid = new MeshInstance3D
        {
            Mesh = mesh,
            Layers = PhaseLayers.SolidRenderLayerFor(_phase),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color },
        };
        AddChild(solid);

        var ghost = new MeshInstance3D
        {
            Mesh = mesh,
            Layers = PhaseLayers.GhostRenderLayerFor(_phase),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(color.R, color.G, color.B, 0.15f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        AddChild(ghost);
    }
}
