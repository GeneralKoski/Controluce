using System.Collections.Generic;
using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Level;

// Pedana a peso: attiva i Targets (IActivatable) finché sopra ci sono
// abbastanza player. PlatePhase != Neutral = conta solo il player di quella fase.
public partial class WeightPlate : Area3D
{
    [Signal] public delegate void StateChangedEventHandler(bool active);

    [Export] public int RequiredPlayers { get; set; } = 1;
    [Export] public Phase PlatePhase { get; set; } = Phase.Neutral;
    [Export] public Godot.Collections.Array<NodePath> Targets { get; set; } = [];

    private readonly HashSet<PlayerController> _pressing = [];
    private MeshInstance3D _plate = null!;
    private AudioStreamPlayer3D _click = null!;
    private bool _active;

    public bool IsActive => _active;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = PhaseLayers.Player;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(1.8f, 0.6f, 1.8f) },
            Position = Vector3.Up * 0.3f,
        });

        Color color = PlatePhase == Phase.Neutral
            ? new Color(0.9f, 0.8f, 0.3f)
            : PhaseGeometry.ColorFor(PlatePhase);
        _plate = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1.8f, 0.12f, 1.8f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color },
            Position = Vector3.Up * 0.06f,
        };
        AddChild(_plate);

        _click = new AudioStreamPlayer3D
        {
            Stream = AudioSynth.Tone(392f, 0.1f),
            VolumeDb = -8f,
        };
        AddChild(_click);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player && Matches(player))
        {
            _pressing.Add(player);
            Refresh();
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController player && _pressing.Remove(player))
            Refresh();
    }

    private bool Matches(PlayerController player) =>
        PlatePhase == Phase.Neutral || player.PlayerPhase == PlatePhase;

    private void Refresh()
    {
        bool active = _pressing.Count >= RequiredPlayers;
        if (active == _active)
            return;

        _active = active;
        _plate.Position = Vector3.Up * (active ? 0.02f : 0.06f);
        _click.PitchScale = active ? 1f : 0.8f;
        _click.Play();
        EmitSignal(SignalName.StateChanged, active);

        foreach (NodePath path in Targets)
        {
            if (GetNodeOrNull(path) is IActivatable target)
                target.SetActivated(active);
        }
    }
}
