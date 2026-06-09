using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Level;

// Checkpoint: si attiva quando un player lo attraversa. I due Marker3D figli
// (SpawnA/SpawnB) sono i punti di respawn.
public partial class Checkpoint : Area3D
{
    [Signal] public delegate void ActivatedEventHandler(Checkpoint checkpoint);

    private MeshInstance3D? _pillar;
    private StandardMaterial3D? _material;

    public Vector3 SpawnPositionA => GetNode<Node3D>("SpawnA").GlobalPosition;
    public Vector3 SpawnPositionB => GetNode<Node3D>("SpawnB").GlobalPosition;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = PhaseLayers.Player;
        AddToGroup("checkpoint");
        BodyEntered += OnBodyEntered;

        _material = new StandardMaterial3D { AlbedoColor = new Color(0.7f, 0.7f, 0.7f) };
        _pillar = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.3f, 2f, 0.3f) },
            MaterialOverride = _material,
            Position = Vector3.Up,
        };
        AddChild(_pillar);
    }

    public void SetActive(bool active)
    {
        if (_material == null)
            return;

        _material.EmissionEnabled = active;
        _material.Emission = new Color(0.2f, 1f, 0.4f);
        _material.AlbedoColor = active ? new Color(0.3f, 1f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController)
            EmitSignal(SignalName.Activated, this);
    }
}
