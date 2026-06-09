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

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = PhaseLayers.Player;
        AddToGroup("exit_zone");
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
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
