using Controluce.Player;
using Godot;

namespace Controluce.Core;

// Vincolo logico di distanza tra i due player (unica simulazione authoritative).
// Niente joint fisico: clamp posizionale + correzione delle velocità divergenti.
public partial class RopeConstraint : Node3D
{
    [Export] public PlayerController? PlayerA { get; set; }
    [Export] public PlayerController? PlayerB { get; set; }
    [Export] public float MaxLength { get; set; } = 6.0f;

    // 0 = corda lasca, 1 = al limite.
    public float Tension { get; private set; }

    public Vector3 AnchorA => PlayerA?.GlobalPosition + Vector3.Up ?? Vector3.Zero;
    public Vector3 AnchorB => PlayerB?.GlobalPosition + Vector3.Up ?? Vector3.Zero;

    public override void _Ready()
    {
        // Deve girare dopo il MoveAndSlide dei player.
        ProcessPhysicsPriority = 10;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (PlayerA == null || PlayerB == null)
            return;

        Vector3 ab = PlayerB.GlobalPosition - PlayerA.GlobalPosition;
        float distance = ab.Length();
        Tension = MaxLength > 0f ? Mathf.Clamp(distance / MaxLength, 0f, 1f) : 0f;

        if (distance <= MaxLength || distance < 1e-4f)
            return;

        Vector3 dir = ab / distance;
        float excess = distance - MaxLength;

        // Chi è in aria viene trascinato; se sono nella stessa condizione, a metà.
        (float weightA, float weightB) = (PlayerA.IsOnFloor(), PlayerB.IsOnFloor()) switch
        {
            (true, false) => (0f, 1f),
            (false, true) => (1f, 0f),
            _ => (0.5f, 0.5f),
        };

        if (weightA > 0f)
            PlayerA.MoveAndCollide(dir * (excess * weightA));
        if (weightB > 0f)
            PlayerB.MoveAndCollide(-dir * (excess * weightB));

        // Annulla la componente di velocità che allontana i due oltre il limite.
        float divergence = (PlayerB.Velocity - PlayerA.Velocity).Dot(dir);
        if (divergence > 0f)
        {
            PlayerA.Velocity += dir * (divergence * weightA);
            PlayerB.Velocity -= dir * (divergence * weightB);
        }
    }
}
