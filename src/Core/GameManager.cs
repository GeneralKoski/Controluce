using Controluce.Level;
using Controluce.Player;
using Godot;

namespace Controluce.Core;

public partial class GameManager : Node
{
    [Export] public PlayerController? Player1 { get; set; }
    [Export] public PlayerController? Player2 { get; set; }
    [Export] public Label? Banner { get; set; }
    [Export] public RopeVisual? Rope { get; set; }
    [Export] public float KillY { get; set; } = -8f;
    // Anti-rage: di default respawnano entrambi insieme, all'ultimo checkpoint.
    [Export] public bool RespawnBothPlayers { get; set; } = true;

    private Vector3 _spawnA;
    private Vector3 _spawnB;
    private Checkpoint? _activeCheckpoint;

    public override void _Ready()
    {
        // Il mondo 3D vive nel viewport root ed è condiviso dai due SubViewport:
        // il root non deve renderizzarlo (lo coprono i due schermi).
        GetViewport().Disable3D = true;

        _spawnA = MoveToSpawn(Player1, "spawn_a");
        _spawnB = MoveToSpawn(Player2, "spawn_b");

        foreach (Node node in GetTree().GetNodesInGroup("exit_zone"))
        {
            if (node is ExitZone exit)
                exit.RoomCompleted += OnRoomCompleted;
        }

        foreach (Node node in GetTree().GetNodesInGroup("checkpoint"))
        {
            if (node is Checkpoint checkpoint)
                checkpoint.Activated += OnCheckpointActivated;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Player1 == null || Player2 == null)
            return;

        bool fellA = Player1.GlobalPosition.Y < KillY;
        bool fellB = Player2.GlobalPosition.Y < KillY;
        if (!fellA && !fellB)
            return;

        if (RespawnBothPlayers || (fellA && fellB))
        {
            Respawn(Player1, _spawnA);
            Respawn(Player2, _spawnB);
        }
        else if (fellA)
            Respawn(Player1, _spawnA);
        else
            Respawn(Player2, _spawnB);

        Rope?.Snap();
    }

    private static void Respawn(PlayerController player, Vector3 position)
    {
        player.GlobalPosition = position;
        player.Velocity = Vector3.Zero;
    }

    private void OnCheckpointActivated(Checkpoint checkpoint)
    {
        if (_activeCheckpoint == checkpoint)
            return;

        _activeCheckpoint?.SetActive(false);
        _activeCheckpoint = checkpoint;
        checkpoint.SetActive(true);
        _spawnA = checkpoint.SpawnPositionA;
        _spawnB = checkpoint.SpawnPositionB;
    }

    private Vector3 MoveToSpawn(PlayerController? player, string group)
    {
        if (player != null && GetTree().GetFirstNodeInGroup(group) is Node3D spawn)
        {
            player.GlobalPosition = spawn.GlobalPosition;
            return spawn.GlobalPosition;
        }
        return player?.GlobalPosition ?? Vector3.Zero;
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
