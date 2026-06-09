using Controluce.Player;
using Godot;

namespace Controluce.Core;

// Vincolo logico di distanza tra i due player (unica simulazione authoritative).
// Niente joint fisico: zona elastica di richiamo, clamp posizionale al limite
// e correzione delle velocità divergenti. Il peso di uno è una meccanica per l'altro.
public partial class RopeConstraint : Node3D
{
    [Export] public PlayerController? PlayerA { get; set; }
    [Export] public PlayerController? PlayerB { get; set; }
    [Export] public float MaxLength { get; set; } = 6.0f;
    [Export] public float MassA { get; set; } = 1.0f;
    [Export] public float MassB { get; set; } = 1.0f;

    // Da che frazione della lunghezza massima inizia il richiamo elastico.
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float SoftZoneStart { get; set; } = 0.85f;
    // Accelerazione massima del richiamo elastico (m/s^2) a corda completamente tesa.
    [Export] public float Elasticity { get; set; } = 14.0f;
    // Smorzamento dell'oscillazione del player appeso (1/s).
    [Export] public float SwingDamping { get; set; } = 0.6f;

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

        float dt = (float)delta;
        Vector3 ab = PlayerB.GlobalPosition - PlayerA.GlobalPosition;
        float distance = ab.Length();
        Tension = MaxLength > 0f ? Mathf.Clamp(distance / MaxLength, 0f, 1f) : 0f;

        if (distance < 1e-4f)
            return;

        Vector3 dir = ab / distance;
        (float weightA, float weightB) = Weights();

        // Richiamo elastico progressivo: si sente prima del limite, leggibile.
        float softStart = SoftZoneStart * MaxLength;
        if (distance > softStart && MaxLength > softStart)
        {
            float intensity = Mathf.Clamp((distance - softStart) / (MaxLength - softStart), 0f, 1f);
            Vector3 pull = dir * (Elasticity * intensity * dt);
            PlayerA.Velocity += pull * weightA;
            PlayerB.Velocity -= pull * weightB;

            DampSwing(PlayerA, dir, dt);
            DampSwing(PlayerB, dir, dt);
        }

        if (distance <= MaxLength)
            return;

        float excess = distance - MaxLength;

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

    // Ripartizione delle correzioni: chi è a terra fa da ancora, in aria
    // conta la massa inversa (il più leggero viene trascinato di più).
    private (float, float) Weights()
    {
        bool groundedA = PlayerA!.IsOnFloor();
        bool groundedB = PlayerB!.IsOnFloor();

        float inverseA = groundedA ? 0f : 1f / Mathf.Max(MassA, 0.01f);
        float inverseB = groundedB ? 0f : 1f / Mathf.Max(MassB, 0.01f);
        float total = inverseA + inverseB;

        if (total <= 0f)
        {
            // Entrambi a terra: si dividono lo strattone in base alla massa.
            inverseA = 1f / Mathf.Max(MassA, 0.01f);
            inverseB = 1f / Mathf.Max(MassB, 0.01f);
            total = inverseA + inverseB;
        }

        return (inverseA / total, inverseB / total);
    }

    private void DampSwing(PlayerController player, Vector3 ropeDir, float dt)
    {
        if (player.IsOnFloor())
            return;

        Vector3 radial = ropeDir * player.Velocity.Dot(ropeDir);
        Vector3 tangential = player.Velocity - radial;
        player.Velocity = radial + tangential * Mathf.Max(0f, 1f - SwingDamping * dt);
    }
}
