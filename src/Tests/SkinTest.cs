using Controluce.Core;
using Controluce.Player;
using Godot;

namespace Controluce.Tests;

// Test integrato su main.tscn: skin procedurali e scambio ruoli.
public partial class SkinTest : Node
{
    private PlayerController _p1 = null!;
    private PlayerController _p2 = null!;
    private int _ticks;
    private bool _failed;

    public override void _Ready()
    {
        Settings.Load();
        Settings.SkinP1 = 1;
        Settings.SkinP2 = 3;
        Settings.SwapRoles = true;

        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _p1 = GetNode<PlayerController>("Main/World/Player1");
        _p2 = GetNode<PlayerController>("Main/World/Player2");
    }

    public override void _PhysicsProcess(double delta)
    {
        _ticks++;
        if (_ticks < 60)
            return;

        Check(_p1.PlayerPhase == Phase.Red, "scambio ruoli: P1 è rosso");
        Check(_p2.PlayerPhase == Phase.Blue, "scambio ruoli: P2 è blu");
        Check(_p1.CollisionMask == PhaseLayers.PlayerCollisionMaskFor(Phase.Red),
            "P1 collide con la geometria rossa");
        Check(_p2.CollisionMask == PhaseLayers.PlayerCollisionMaskFor(Phase.Blue),
            "P2 collide con la geometria blu");

        Check(_p1.GlobalPosition.DistanceTo(new Vector3(-1.5f, 1f, -2f)) < 1.5f,
            $"P1 resta sul proprio lato di spawn ({_p1.GlobalPosition})");
        Check(_p2.GlobalPosition.DistanceTo(new Vector3(1.5f, 1f, -2f)) < 1.5f,
            $"P2 resta sul proprio lato di spawn ({_p2.GlobalPosition})");

        var skin1 = _p1.GetNode<MeshInstance3D>("MeshInstance3D").GetNodeOrNull<Node3D>("Skin");
        var skin2 = _p2.GetNode<MeshInstance3D>("MeshInstance3D").GetNodeOrNull<Node3D>("Skin");
        Check(skin1 != null && skin1.GetChildCount() == 2, "skin Antenna su P1 (2 accessori)");
        Check(skin2 != null && skin2.GetChildCount() == 2, "skin Corna su P2 (2 accessori)");

        var rig1 = GetNode<CameraRig>("Main/Split/ViewP1/ViewportP1/CameraRig");
        Check(rig1.ViewPhase == Phase.Red, "camera P1 vede la fase rossa");

        GD.Print(_failed ? "SKIN TEST: FAIL" : "SKIN TEST: PASS");
        GetTree().Quit(_failed ? 1 : 0);
    }

    private void Check(bool condition, string label)
    {
        GD.Print($"{(condition ? "OK " : "FAIL")} - {label}");
        if (!condition)
            _failed = true;
    }
}
