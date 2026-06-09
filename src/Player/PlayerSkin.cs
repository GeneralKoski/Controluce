using System.Collections.Generic;
using Controluce.Core;
using Godot;

namespace Controluce.Player;

// Skin procedurali dei player: solo primitive generate in codice, nessun
// asset binario. La skin cambia accessori; il colore di ruolo (blu/rosso)
// resta legato alla fase e si scambia con l'opzione "scambia ruoli".
public static class PlayerSkin
{
    public static readonly string[] Names = ["Classica", "Antenna", "Tuba", "Corna", "Aureola"];
    public static int Count => Names.Length;

    private static readonly Dictionary<Phase, StandardMaterial3D> _bodyMaterials = new();
    private static StandardMaterial3D? _goldMaterial;
    private static StandardMaterial3D? _darkMaterial;
    private static StandardMaterial3D? _boneMaterial;

    public static StandardMaterial3D BodyMaterial(Phase phase)
    {
        if (_bodyMaterials.TryGetValue(phase, out var cached))
            return cached;

        Color color = phase == Phase.Blue ? new Color(0.3f, 0.55f, 1f) : new Color(1f, 0.35f, 0.3f);
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.45f,
            EmissionEnabled = true,
            Emission = color * 0.25f,
            RimEnabled = true,
            Rim = 0.6f,
            RimTint = 0.15f,
        };
        _bodyMaterials[phase] = material;
        return material;
    }

    // Applica colore di ruolo e accessori della skin alla mesh del player.
    // Riapplicabile a runtime (es. quando arriva la scelta del peer online).
    public static void Apply(MeshInstance3D visual, Phase rolePhase, int skin)
    {
        visual.MaterialOverride = BodyMaterial(rolePhase);

        if (visual.GetNodeOrNull("Skin") is Node old)
        {
            visual.RemoveChild(old);
            old.QueueFree();
        }

        var root = new Node3D { Name = "Skin" };
        visual.AddChild(root);

        switch (Mathf.PosMod(skin, Count))
        {
            case 1: BuildAntenna(root); break;
            case 2: BuildTuba(root); break;
            case 3: BuildCorna(root); break;
            case 4: BuildAureola(root); break;
        }
    }

    private static StandardMaterial3D Gold() => _goldMaterial ??= new StandardMaterial3D
    {
        AlbedoColor = new Color(1f, 0.85f, 0.4f),
        Roughness = 0.3f,
        EmissionEnabled = true,
        Emission = new Color(1f, 0.8f, 0.35f) * 0.8f,
    };

    private static StandardMaterial3D Dark() => _darkMaterial ??= new StandardMaterial3D
    {
        AlbedoColor = new Color(0.13f, 0.13f, 0.16f),
        Roughness = 0.55f,
    };

    private static StandardMaterial3D Bone() => _boneMaterial ??= new StandardMaterial3D
    {
        AlbedoColor = new Color(0.92f, 0.9f, 0.82f),
        Roughness = 0.7f,
    };

    private static void BuildAntenna(Node3D root)
    {
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.03f, Height = 0.45f, RadialSegments = 8 },
            MaterialOverride = Dark(),
            Position = new Vector3(0, 1.15f, 0),
        });
        root.AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.08f, Height = 0.16f, RadialSegments = 16, Rings = 8 },
            MaterialOverride = Gold(),
            Position = new Vector3(0, 1.42f, 0),
        });
    }

    private static void BuildTuba(Node3D root)
    {
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.42f, BottomRadius = 0.42f, Height = 0.05f, RadialSegments = 24 },
            MaterialOverride = Dark(),
            Position = new Vector3(0, 0.98f, 0),
        });
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.26f, BottomRadius = 0.26f, Height = 0.42f, RadialSegments = 24 },
            MaterialOverride = Dark(),
            Position = new Vector3(0, 1.2f, 0),
        });
    }

    private static void BuildCorna(Node3D root)
    {
        foreach (float side in new[] { -1f, 1f })
        {
            root.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0f, BottomRadius = 0.09f, Height = 0.35f, RadialSegments = 8 },
                MaterialOverride = Bone(),
                Position = new Vector3(side * 0.3f, 0.95f, 0),
                Rotation = new Vector3(0, 0, -side * 0.5f),
            });
        }
    }

    private static void BuildAureola(Node3D root)
    {
        root.AddChild(new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.22f, OuterRadius = 0.3f, RingSegments = 24, Rings = 12 },
            MaterialOverride = Gold(),
            Position = new Vector3(0, 1.35f, 0),
        });
    }
}
