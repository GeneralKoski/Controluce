using Godot;

namespace Controluce.Player;

// Layer di presentazione del player: squash & stretch, occhi orientati
// al moto, lean e polvere all'atterraggio. Nessuna logica di gioco.
public partial class PlayerVisual : MeshInstance3D
{
    private CharacterBody3D _body = null!;
    private CpuParticles3D _dust = null!;
    private bool _wasAirborne;
    private float _deform;
    private float _facing;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
        BuildEyes();
        BuildDust();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool airborne = !_body.IsOnFloor();

        float target = airborne
            ? Mathf.Clamp(Mathf.Abs(_body.Velocity.Y) / 14f, 0f, 1f) * 0.25f
            : 0f;
        if (_wasAirborne && !airborne)
        {
            _deform = -0.25f; // impatto: schiacciato, poi torna su elasticamente
            _dust.Restart();
        }
        _wasAirborne = airborne;

        _deform = Mathf.Lerp(_deform, target, 1f - Mathf.Exp(-12f * dt));

        float y = 1f + _deform;
        float xz = 1f - _deform * 0.5f;
        Scale = new Vector3(xz, y, xz);
        Position = new Vector3(0, y, 0); // i piedi restano a terra

        Vector3 horizontal = _body.Velocity with { Y = 0 };
        if (horizontal.Length() > 0.8f)
        {
            float targetYaw = Mathf.Atan2(-horizontal.X, -horizontal.Z);
            _facing = Mathf.LerpAngle(_facing, targetYaw, 1f - Mathf.Exp(-10f * dt));
        }

        float pitchLean = -Mathf.Clamp(horizontal.Length() / 14f, 0f, 0.22f);
        Rotation = new Vector3(pitchLean, _facing, 0);
    }

    private void BuildEyes()
    {
        var white = new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        var black = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.08f, 0.08f, 0.1f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        foreach (float side in new[] { -1f, 1f })
        {
            AddChild(new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.09f, Height = 0.18f },
                MaterialOverride = white,
                Position = new Vector3(side * 0.17f, 0.32f, -0.42f),
            });
            AddChild(new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f },
                MaterialOverride = black,
                Position = new Vector3(side * 0.16f, 0.32f, -0.49f),
            });
        }
    }

    private void BuildDust()
    {
        _dust = new CpuParticles3D
        {
            Emitting = false,
            OneShot = true,
            Amount = 14,
            Lifetime = 0.45f,
            Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f },
            Position = new Vector3(0, 0.15f, 0),
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.3f,
            Direction = Vector3.Up,
            Spread = 60f,
            InitialVelocityMin = 1f,
            InitialVelocityMax = 2.5f,
            Gravity = new Vector3(0, -6f, 0),
            ScaleAmountMin = 0.6f,
            ScaleAmountMax = 1.4f,
        };
        if (_dust.Mesh is SphereMesh mesh)
            mesh.Material = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.75f, 0.73f, 0.7f),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            };

        // Figlia del corpo (non della mesh) per non ereditare squash e rotazioni.
        _body.CallDeferred(Node.MethodName.AddChild, _dust);
    }
}
