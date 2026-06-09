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

    // Tira-corda: tenendo premuto il tasto la corda si riavvolge fino a MinLength
    // (per recuperare il partner appeso); al rilascio si riallunga gradualmente.
    // MinLength non deve scendere sotto l'ingombro dei due corpi (~2.2 m),
    // altrimenti il vincolo li schiaccia l'uno nell'altro.
    [Export] public float MinLength { get; set; } = 2.2f;
    [Export] public float PullSpeed { get; set; } = 2.5f;
    [Export] public float ReleaseSpeed { get; set; } = 3.0f;

    // Da che frazione della lunghezza massima inizia il richiamo elastico.
    [Export(PropertyHint.Range, "0.5,1,0.01")] public float SoftZoneStart { get; set; } = 0.85f;
    // Accelerazione massima del richiamo elastico (m/s^2) a corda completamente tesa.
    [Export] public float Elasticity { get; set; } = 14.0f;
    // Smorzamento dell'oscillazione del player appeso (1/s).
    // Basso: il dondolio deve restare vivo e controllabile.
    [Export] public float SwingDamping { get; set; } = 0.15f;

    // 0 = corda lasca, 1 = al limite.
    public float Tension { get; private set; }

    // Lunghezza effettiva corrente (MaxLength quando nessuno tira).
    public float CurrentLength { get; private set; }

    // True mentre qualcuno sta riavvolgendo la corda.
    public bool IsReeling { get; private set; }

    public Vector3 AnchorA => PlayerA?.GlobalPosition + Vector3.Up ?? Vector3.Zero;
    public Vector3 AnchorB => PlayerB?.GlobalPosition + Vector3.Up ?? Vector3.Zero;

    public override void _Ready()
    {
        // Deve girare dopo il MoveAndSlide dei player.
        ProcessPhysicsPriority = 10;
        CurrentLength = MaxLength;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (PlayerA == null || PlayerB == null)
            return;

        float dt = (float)delta;

        bool pulling = PlayerA.IsPullingRope || PlayerB.IsPullingRope;
        IsReeling = pulling;
        CurrentLength = Mathf.MoveToward(
            CurrentLength,
            pulling ? Mathf.Min(MinLength, MaxLength) : MaxLength,
            (pulling ? PullSpeed : ReleaseSpeed) * dt);

        Vector3 ab = PlayerB.GlobalPosition - PlayerA.GlobalPosition;
        float distance = ab.Length();
        Tension = CurrentLength > 0f ? Mathf.Clamp(distance / CurrentLength, 0f, 1f) : 0f;

        if (distance < 1e-4f)
            return;

        Vector3 dir = ab / distance;
        (float weightA, float weightB) = Weights();

        // Stato "appeso": in aria, corda quasi tesa, ancorato sopra di sé.
        bool taut = distance > 0.9f * CurrentLength;
        PlayerA.SetRopeHang(taut && !PlayerA.IsOnFloor() && dir.Y > 0.3f, dir);
        PlayerB.SetRopeHang(taut && !PlayerB.IsOnFloor() && -dir.Y > 0.3f, -dir);

        // Richiamo elastico progressivo: si sente prima del limite, leggibile.
        float softStart = SoftZoneStart * CurrentLength;
        if (distance > softStart && CurrentLength > softStart)
        {
            float intensity = Mathf.Clamp((distance - softStart) / (CurrentLength - softStart), 0f, 1f);
            Vector3 pull = dir * (Elasticity * intensity * dt);
            PlayerA.Velocity += pull * weightA;
            PlayerB.Velocity -= pull * weightB;

            DampSwing(PlayerA, dir, dt);
            DampSwing(PlayerB, dir, dt);
        }

        if (distance <= CurrentLength)
            return;

        float excess = distance - CurrentLength;

        if (weightA > 0f)
            Correct(PlayerA, dir * (excess * weightA));
        if (weightB > 0f)
            Correct(PlayerB, -dir * (excess * weightB));

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
        bool groundedA = IsAnchored(PlayerA!, PlayerB!);
        bool groundedB = IsAnchored(PlayerB!, PlayerA!);

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

    // Stato ricevuto dal server quando si gioca da client (sim locale spenta).
    public void ApplyNetworkState(float currentLength, float tension)
    {
        CurrentLength = currentLength;
        Tension = tension;
    }

    // Un player conta come ancora solo se sta a terra su geometria vera:
    // stare in piedi sull'altro player non vale (eviterebbe la "scala infinita").
    private static bool IsAnchored(PlayerController player, PlayerController other)
    {
        if (!player.IsOnFloor())
            return false;

        for (int i = 0; i < player.GetSlideCollisionCount(); i++)
        {
            var collision = player.GetSlideCollision(i);
            if (collision.GetCollider() == other && collision.GetNormal().Y > 0.5f)
                return false;
        }
        return true;
    }

    // Clamp posizionale che scivola lungo gli ostacoli invece di incastrarsi
    // (es. risalire aggirando il bordo di una sporgenza).
    private static void Correct(PlayerController player, Vector3 motion)
    {
        var collision = player.MoveAndCollide(motion);
        if (collision != null)
            player.MoveAndCollide(collision.GetRemainder().Slide(collision.GetNormal()));
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
