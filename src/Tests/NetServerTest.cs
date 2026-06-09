using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Lato server del test loopback: aspetta che il client connesso muova P2.
// Lanciare con CONTROLUCE_MODE=server CONTROLUCE_PORT=<porta>.
public partial class NetServerTest : Node
{
    private PlayerController _p2 = null!;
    private Vector3 _last;
    private float _traveled;
    private int _ticks;
    private bool _moved;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _p2 = GetNode<PlayerController>("Main/World/Player2");
        _last = _p2.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        _traveled += (_p2.GlobalPosition - _last).Length();
        _last = _p2.GlobalPosition;
        if (!_moved && _traveled > 3f)
        {
            _moved = true;
            GD.Print($"OK  - P2 mosso dai comandi del client ({_traveled:F2} m percorsi)");
        }

        // Resta vivo abbastanza a lungo da servire snapshot al client.
        if (_moved && _ticks > 1000)
        {
            GD.Print("NET SERVER TEST: PASS");
            GetTree().Quit(0);
            return;
        }

        if (_ticks > 1500) // 25 secondi
        {
            GD.Print("FAIL - P2 non si è mosso (nessun comando ricevuto)");
            GD.Print("NET SERVER TEST: FAIL");
            GetTree().Quit(1);
        }
    }
}
