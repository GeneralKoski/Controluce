using Godot;

namespace Controluce.Core;

// Corda visiva (catena verlet): pura decorazione che segue i due ancoraggi.
// Non influenza la fisica: il vincolo è in RopeConstraint.
public partial class RopeVisual : MultiMeshInstance3D
{
    [Export] public RopeConstraint? Constraint { get; set; }
    [Export] public int Segments { get; set; } = 16;
    [Export] public float Damping { get; set; } = 0.92f;
    [Export] public float SagSlack { get; set; } = 0.4f;

    private static readonly Color SlackColor = new(0.45f, 0.33f, 0.2f);
    private static readonly Color TautColor = new(1f, 0.25f, 0.15f);

    private Vector3[] _points = [];
    private Vector3[] _previous = [];
    private StandardMaterial3D _material = null!;
    private AudioStreamPlayer3D _creakAudio = null!;
    private float _creakCooldown;

    public override void _Ready()
    {
        int count = Segments + 1;
        _points = new Vector3[count];
        _previous = new Vector3[count];

        _material = new StandardMaterial3D { AlbedoColor = SlackColor };
        var mesh = new CylinderMesh
        {
            TopRadius = 0.04f,
            BottomRadius = 0.04f,
            Height = 1f,
            RadialSegments = 6,
            Material = _material,
        };

        _creakAudio = new AudioStreamPlayer3D
        {
            Stream = AudioSynth.Creak(),
            VolumeDb = -6f,
        };
        AddChild(_creakAudio);

        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = Segments,
        };

        TopLevel = true;
        GlobalTransform = Transform3D.Identity;

        if (Constraint != null)
            ResetTo(Constraint.AnchorA, Constraint.AnchorB);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Constraint == null)
            return;

        float dt = (float)delta;
        Vector3 anchorA = Constraint.AnchorA;
        Vector3 anchorB = Constraint.AnchorB;
        // La corda visiva segue la distanza reale (più un filo di abbondanza),
        // così pende poco quando è lasca e si tende man mano che ci si allontana.
        float visualLength = Mathf.Min(Constraint.CurrentLength, anchorA.DistanceTo(anchorB) + SagSlack);
        float restLength = visualLength / Segments;

        for (int i = 1; i < Segments; i++)
        {
            Vector3 velocity = (_points[i] - _previous[i]) * Damping;
            _previous[i] = _points[i];
            _points[i] += velocity + Vector3.Down * (9.8f * dt * dt);
        }

        _points[0] = anchorA;
        _points[Segments] = anchorB;

        for (int iteration = 0; iteration < 4; iteration++)
        {
            for (int i = 0; i < Segments; i++)
            {
                Vector3 segment = _points[i + 1] - _points[i];
                float length = segment.Length();
                if (length < 1e-5f || length <= restLength)
                    continue;

                Vector3 correction = segment * ((length - restLength) / length);
                bool pinnedA = i == 0;
                bool pinnedB = i + 1 == Segments;

                if (pinnedA && pinnedB)
                    continue;
                if (pinnedA)
                    _points[i + 1] -= correction;
                else if (pinnedB)
                    _points[i] += correction;
                else
                {
                    _points[i] += correction * 0.5f;
                    _points[i + 1] -= correction * 0.5f;
                }
            }
        }

        UpdateInstances();
        UpdateTensionFeedback(dt, anchorA, anchorB);
    }

    private void UpdateTensionFeedback(float dt, Vector3 anchorA, Vector3 anchorB)
    {
        float tension = Constraint!.Tension;
        float emphasis = Mathf.Clamp((tension - 0.7f) / 0.3f, 0f, 1f);
        _material.AlbedoColor = SlackColor.Lerp(TautColor, emphasis);

        _creakCooldown -= dt;
        if (tension > 0.92f && _creakCooldown <= 0f)
        {
            _creakCooldown = 0.9f;
            _creakAudio.GlobalPosition = (anchorA + anchorB) * 0.5f;
            _creakAudio.PitchScale = 0.85f + GD.Randf() * 0.3f;
            _creakAudio.Play();
        }
    }

    public void Snap()
    {
        if (Constraint != null)
            ResetTo(Constraint.AnchorA, Constraint.AnchorB);
    }

    private void ResetTo(Vector3 a, Vector3 b)
    {
        for (int i = 0; i <= Segments; i++)
        {
            _points[i] = a.Lerp(b, i / (float)Segments);
            _previous[i] = _points[i];
        }
    }

    private void UpdateInstances()
    {
        for (int i = 0; i < Segments; i++)
        {
            Vector3 from = _points[i];
            Vector3 to = _points[i + 1];
            Vector3 segment = to - from;
            float length = segment.Length();

            if (length < 1e-5f)
            {
                Multimesh!.SetInstanceTransform(i, new Transform3D(Basis.Identity.Scaled(Vector3.One * 0.001f), from));
                continue;
            }

            Vector3 yAxis = segment / length;
            Vector3 reference = Mathf.Abs(yAxis.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
            Vector3 xAxis = reference.Cross(yAxis).Normalized();
            Vector3 zAxis = xAxis.Cross(yAxis);
            var basis = new Basis(xAxis, yAxis * length, zAxis);

            Multimesh!.SetInstanceTransform(i, new Transform3D(basis, (from + to) * 0.5f));
        }
    }
}
