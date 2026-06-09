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

    private Vector3[] _points = [];
    private Vector3[] _previous = [];

    public override void _Ready()
    {
        int count = Segments + 1;
        _points = new Vector3[count];
        _previous = new Vector3[count];

        var mesh = new CylinderMesh
        {
            TopRadius = 0.04f,
            BottomRadius = 0.04f,
            Height = 1f,
            RadialSegments = 6,
            Material = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.33f, 0.2f) },
        };

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
        float visualLength = Mathf.Min(Constraint.MaxLength, anchorA.DistanceTo(anchorB) + SagSlack);
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
