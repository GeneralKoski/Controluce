using Godot;

namespace Controluce.Player;

// Layer di presentazione del player: squash & stretch sul salto/atterraggio
// e lean nella direzione del moto. Nessuna logica di gioco.
public partial class PlayerVisual : MeshInstance3D
{
    private CharacterBody3D _body = null!;
    private bool _wasAirborne;
    private float _deform;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool airborne = !_body.IsOnFloor();

        float target = airborne
            ? Mathf.Clamp(Mathf.Abs(_body.Velocity.Y) / 14f, 0f, 1f) * 0.25f
            : 0f;
        if (_wasAirborne && !airborne)
            _deform = -0.25f; // impatto: schiacciato, poi torna su elasticamente
        _wasAirborne = airborne;

        _deform = Mathf.Lerp(_deform, target, 1f - Mathf.Exp(-12f * dt));

        float y = 1f + _deform;
        float xz = 1f - _deform * 0.5f;
        Scale = new Vector3(xz, y, xz);
        Position = new Vector3(0, y, 0); // i piedi restano a terra

        Vector3 lean = (_body.Velocity with { Y = 0 }) / 12f;
        Rotation = new Vector3(
            Mathf.Clamp(lean.Z, -0.25f, 0.25f),
            0,
            Mathf.Clamp(-lean.X, -0.25f, 0.25f));
    }
}
