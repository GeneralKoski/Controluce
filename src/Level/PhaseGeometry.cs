using Controluce.Core;
using Godot;

namespace Controluce.Level;

// Costruzione condivisa di un blocco di fase: collision shape, mesh solida
// e mesh "fantasma" per la fase opposta. Usata da PhaseBlock e MovingPlatform.
public static class PhaseGeometry
{
    // Contatore di ricostruzioni, esposto per il profiling (test_perf).
    public static long BuildCount { get; private set; }

    public static readonly Color BlueColor = new(0.25f, 0.5f, 1f);
    public static readonly Color RedColor = new(1f, 0.3f, 0.25f);
    public static readonly Color NeutralColor = new(0.55f, 0.57f, 0.6f);

    public static Color ColorFor(Phase phase) => phase switch
    {
        Phase.Blue => BlueColor,
        Phase.Red => RedColor,
        _ => NeutralColor,
    };

    private static ImageTexture? _gridTexture;
    private static readonly System.Collections.Generic.Dictionary<Phase, StandardMaterial3D> _solidMaterials = new();
    private static readonly System.Collections.Generic.Dictionary<Phase, ShaderMaterial> _ghostMaterials = new();
    private static Shader? _ghostShader;

    // Materiale solido condiviso per fase: i blocchi di fase emettono luce
    // tenue (leggibilità + glow), il rim li staglia in controluce.
    private static StandardMaterial3D SolidMaterial(Phase phase)
    {
        if (_solidMaterials.TryGetValue(phase, out var cached))
            return cached;

        Color color = ColorFor(phase);
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            AlbedoTexture = GridTexture(),
            Uv1Triplanar = true,
            Uv1Scale = new Vector3(0.5f, 0.5f, 0.5f),
            Roughness = 0.85f,
        };
        if (phase != Phase.Neutral)
        {
            material.EmissionEnabled = true;
            material.Emission = color * 0.35f;
            material.RimEnabled = true;
            material.Rim = 0.4f;
            material.RimTint = 0.3f;
        }
        _solidMaterials[phase] = material;
        return material;
    }

    // Materiale fantasma condiviso: fresnel additivo che illumina i bordi,
    // così la geometria dell'altra fase resta leggibile ma non invadente.
    private static ShaderMaterial GhostMaterial(Phase phase)
    {
        if (_ghostMaterials.TryGetValue(phase, out var cached))
            return cached;

        _ghostShader ??= new Shader
        {
            Code = """
                shader_type spatial;
                render_mode blend_mix, depth_draw_never, cull_back, unshaded;

                uniform vec4 tint : source_color = vec4(1.0);

                void fragment() {
                    float fres = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 2.5);
                    ALBEDO = tint.rgb * (0.55 + 0.45 * fres);
                    ALPHA = clamp(0.16 + 0.5 * fres, 0.0, 1.0);
                }
                """,
        };

        var material = new ShaderMaterial { Shader = _ghostShader };
        material.SetShaderParameter("tint", ColorFor(phase));
        _ghostMaterials[phase] = material;
        return material;
    }

    // Griglia tenue generata in codice: aiuta a leggere distanze e velocità.
    private static ImageTexture GridTexture()
    {
        if (_gridTexture != null)
            return _gridTexture;

        const int size = 64;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        var line = new Color(0.78f, 0.78f, 0.82f);
        for (int i = 0; i < size; i++)
        {
            image.SetPixel(i, 0, line);
            image.SetPixel(0, i, line);
        }

        _gridTexture = ImageTexture.CreateFromImage(image);
        return _gridTexture;
    }

    // Cambio di sola fase (toggle/attivazione): aggiorna layer e materiali
    // in place, senza ricreare nodi e collision shape. Ritorna false se il
    // blocco va ricostruito da zero (mai costruito, o era/diventa neutro).
    public static bool Recolor(PhysicsBody3D body, Phase phase)
    {
        if (phase == Phase.Neutral
            || body.GetNodeOrNull<MeshInstance3D>("Solid") is not { } solid
            || body.GetNodeOrNull<MeshInstance3D>("Ghost") is not { } ghost)
            return false;

        body.CollisionLayer = PhaseLayers.GeometryLayerFor(phase);
        solid.Layers = PhaseLayers.SolidRenderLayerFor(phase);
        solid.MaterialOverride = SolidMaterial(phase);
        ghost.Layers = PhaseLayers.GhostRenderLayerFor(phase);
        ghost.MaterialOverride = GhostMaterial(phase);
        return true;
    }

    // Ritorna la mesh solida (per eventuali effetti, es. blink dei blocchi a tempo).
    public static MeshInstance3D Build(PhysicsBody3D body, Phase phase, Vector3 size)
    {
        BuildCount++;
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

        var mesh = new BoxMesh { Size = size };

        var solid = new MeshInstance3D
        {
            Name = "Solid",
            Mesh = mesh,
            Layers = PhaseLayers.SolidRenderLayerFor(phase),
            MaterialOverride = SolidMaterial(phase),
        };
        body.AddChild(solid);

        if (phase == Phase.Neutral)
            return solid;

        var ghost = new MeshInstance3D
        {
            Name = "Ghost",
            Mesh = mesh,
            Layers = PhaseLayers.GhostRenderLayerFor(phase),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = GhostMaterial(phase),
        };
        body.AddChild(ghost);
        return solid;
    }
}
