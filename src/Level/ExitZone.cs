using System.Collections.Generic;
using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Level;

// Uscita della stanza: si attiva solo quando ENTRAMBI i player sono dentro.
public partial class ExitZone : Area3D
{
    [Signal] public delegate void RoomCompletedEventHandler();

    private readonly HashSet<PlayerController> _inside = [];
    private bool _completed;
    private StandardMaterial3D? _beamMaterial;
    private float _pulse;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = PhaseLayers.Player;
        AddToGroup("exit_zone");
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        BuildBeam();
    }

    // Portale: colonna di luce calda, visibile a entrambi i player,
    // che segna l'obiettivo della stanza. Pulsa piano; si accende al completamento.
    private void BuildBeam()
    {
        _beamMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.8f, 0.4f, 0.16f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            EmissionEnabled = true,
            Emission = new Color(1f, 0.75f, 0.35f) * 0.6f,
        };
        AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh
            {
                TopRadius = 1.1f,
                BottomRadius = 1.4f,
                Height = 4f,
                RadialSegments = 24,
                Rings = 1,
            },
            MaterialOverride = _beamMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Position = Vector3.Up * 0.5f,
        });
    }

    public override void _Process(double delta)
    {
        if (_beamMaterial == null)
            return;

        _pulse += (float)delta;
        float alpha = _completed
            ? 0.4f
            : 0.13f + 0.05f * Mathf.Sin(_pulse * 2f);
        _beamMaterial.AlbedoColor = _beamMaterial.AlbedoColor with { A = alpha };
        _beamMaterial.Emission = new Color(1f, 0.75f, 0.35f) * (_completed ? 1.4f : 0.6f);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not PlayerController player)
            return;

        _inside.Add(player);
        if (!_completed && _inside.Count >= 2)
        {
            _completed = true;
            EmitSignal(SignalName.RoomCompleted);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController player)
            _inside.Remove(player);
    }
}
