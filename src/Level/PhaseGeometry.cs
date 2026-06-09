using Controluce.Core;
using Godot;

namespace Controluce.Level;

// Costruzione condivisa di un blocco di fase: collision shape, mesh solida
// e mesh "fantasma" per la fase opposta. Usata da PhaseBlock e MovingPlatform.
public static class PhaseGeometry
{
    public static readonly Color BlueColor = new(0.25f, 0.5f, 1f);
    public static readonly Color RedColor = new(1f, 0.3f, 0.25f);
    public static readonly Color NeutralColor = new(0.55f, 0.57f, 0.6f);

    public static Color ColorFor(Phase phase) => phase switch
    {
        Phase.Blue => BlueColor,
        Phase.Red => RedColor,
        _ => NeutralColor,
    };

    // Ritorna la mesh solida (per eventuali effetti, es. blink dei blocchi a tempo).
    public static MeshInstance3D Build(PhysicsBody3D body, Phase phase, Vector3 size)
    {
        foreach (Node child in body.GetChildren())
        {
            if (child is CollisionShape3D or MeshInstance3D)
            {
                body.RemoveChild(child);
                child.QueueFree();
            }
        }

        body.CollisionLayer = PhaseLayers.GeometryLayerFor(phase);
        body.CollisionMask = 0;

        var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = size } };
        body.AddChild(shape);

        Color color = ColorFor(phase);
        var mesh = new BoxMesh { Size = size };

        var solid = new MeshInstance3D
        {
            Mesh = mesh,
            Layers = PhaseLayers.SolidRenderLayerFor(phase),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color },
        };
        body.AddChild(solid);

        if (phase == Phase.Neutral)
            return solid;

        var ghost = new MeshInstance3D
        {
            Mesh = mesh,
            Layers = PhaseLayers.GhostRenderLayerFor(phase),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(color.R, color.G, color.B, 0.15f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        body.AddChild(ghost);
        return solid;
    }
}
