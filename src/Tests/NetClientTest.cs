using Controluce.Core;
using Controluce.Net;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Lato client del test loopback: invia comandi di movimento e verifica
// che gli snapshot del server muovano il P2 renderizzato localmente.
// Lanciare con CONTROLUCE_MODE=client CONTROLUCE_PORT=<porta>.
public partial class NetClientTest : Node
{
    private PlayerController _p2 = null!;
    private Vector3 _last;
    private bool _tracking;
    private float _traveled;
    private int _ticks;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _p2 = GetNode<PlayerController>("Main/World/Player2");

        _network = GetNode<NetworkManager>("Main/Network");
        // Saltella sul posto: movimento continuo senza avvicinarsi ai bordi.
        _network.LocalCommandOverride = () => new PlayerCommand(Vector2.Zero, true, false, false);
    }

    private NetworkManager _network = null!;

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;

        if (_ticks == 120) // dopo connessione e primi snapshot
        {
            _last = _p2.GlobalPosition;
            _tracking = true;
        }

        if (_tracking)
        {
            _traveled += (_p2.GlobalPosition - _last).Length();
            _last = _p2.GlobalPosition;

            if (_traveled > 4f)
            {
                GD.Print($"OK  - snapshot ricevuti, P2 percorre {_traveled:F2} m");
                GD.Print("NET CLIENT TEST: PASS");
                GetTree().Quit(0);
                return;
            }
        }

        if (_ticks > 1200) // 20 secondi
        {
            GD.Print("FAIL - P2 locale non si muove (snapshot assenti?)");
            GD.Print("NET CLIENT TEST: FAIL");
            GetTree().Quit(1);
        }
    }
}
