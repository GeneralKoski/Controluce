using Controluce.Core;
using Godot;

namespace Controluce.Level;

[Tool]
public partial class PhaseBlock : StaticBody3D
{
    private static readonly Color BlueColor = new(0.25f, 0.5f, 1f);
    private static readonly Color RedColor = new(1f, 0.3f, 0.25f);
    private static readonly Color NeutralColor = new(0.55f, 0.57f, 0.6f);

    private Phase _phase = Phase.Blue;
    private Vector3 _size = new(2f, 0.5f, 2f);
    private bool _snapping;

    // Snap della posizione su griglia (solo in editor), per comporre stanze in fretta.
    [Export] public bool SnapToGrid { get; set; } = true;
    [Export] public float GridStep { get; set; } = 0.25f;

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

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            SetNotifyLocalTransform(true);
        Rebuild();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationLocalTransformChanged
            || !Engine.IsEditorHint() || !SnapToGrid || _snapping || GridStep <= 0f)
            return;

        _snapping = true;
        Position = (Position / GridStep).Round() * GridStep;
        _snapping = false;
    }

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

        Color color = _phase switch
        {
            Phase.Blue => BlueColor,
            Phase.Red => RedColor,
            _ => NeutralColor,
        };
        var mesh = new BoxMesh { Size = _size };

        var solid = new MeshInstance3D
        {
            Mesh = mesh,
            Layers = PhaseLayers.SolidRenderLayerFor(_phase),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color },
        };
        AddChild(solid);

        if (_phase == Phase.Neutral)
            return;

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
