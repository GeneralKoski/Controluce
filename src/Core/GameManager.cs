using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Core;

public partial class GameManager : Node
{
    [Export] public PlayerController? Player1 { get; set; }
    [Export] public PlayerController? Player2 { get; set; }
    [Export] public Label? Banner { get; set; }

    public override void _Ready()
    {
        // Il mondo 3D vive nel viewport root ed è condiviso dai due SubViewport:
        // il root non deve renderizzarlo (lo coprono i due schermi).
        GetViewport().Disable3D = true;

        MoveToSpawn(Player1, "spawn_a");
        MoveToSpawn(Player2, "spawn_b");

        foreach (Node node in GetTree().GetNodesInGroup("exit_zone"))
        {
            if (node is ExitZone exit)
                exit.RoomCompleted += OnRoomCompleted;
        }
    }

    private void MoveToSpawn(PlayerController? player, string group)
    {
        if (player != null && GetTree().GetFirstNodeInGroup(group) is Node3D spawn)
            player.GlobalPosition = spawn.GlobalPosition;
    }

    private void OnRoomCompleted()
    {
        GD.Print("Stanza completata!");
        if (Banner != null)
        {
            Banner.Text = "Stanza completata!";
            Banner.Visible = true;
        }
    }
}
