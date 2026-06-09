using Godot;

namespace Controluce.Player;

// Freccia sul bordo dello schermo che indica dove sta il partner quando
// esce dalla visuale. Puro layer di presentazione: legge solo posizioni.
public partial class PartnerIndicator : Control
{
    [Export] public Camera3D? Camera { get; set; }
    [Export] public PlayerController? Partner { get; set; }

    private const float Margin = 36f;
    private bool _occluded;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Process(double delta)
    {
        // Il partner conta come nascosto anche quando è nel frustum ma
        // dietro la geometria che questo viewer vede solida.
        if (Camera != null && Partner != null && Partner.IsInsideTree())
        {
            uint mask = Core.PhaseLayers.Neutral
                | Core.PhaseLayers.GeometryLayerFor(Core.PhaseLayers.Opposite(Partner.PlayerPhase));
            var query = PhysicsRayQueryParameters3D.Create(
                Camera.GlobalPosition, Partner.GlobalPosition + Vector3.Up, mask);
            _occluded = Partner.GetWorld3D().DirectSpaceState.IntersectRay(query).Count > 0;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Camera == null || Partner == null || !Camera.IsInsideTree())
            return;

        Vector3 target = Partner.GlobalPosition + Vector3.Up;
        bool behind = Camera.IsPositionBehind(target);
        Vector2 screen = Camera.UnprojectPosition(target);

        Color color = Partner.PlayerPhase == Core.Phase.Blue
            ? new Color(0.35f, 0.6f, 1f)
            : new Color(1f, 0.4f, 0.35f);

        var inner = new Rect2(Margin, Margin, Size.X - Margin * 2, Size.Y - Margin * 2);
        if (!behind && inner.HasPoint(screen))
        {
            // In vista ma dietro un muro: sagoma "a raggi X" sul punto.
            if (_occluded)
            {
                DrawArc(screen, 16f, 0, Mathf.Tau, 24, color with { A = 0.8f }, 3f);
                DrawCircle(screen, 5f, color with { A = 0.8f });
            }
            return;
        }

        Vector2 center = Size * 0.5f;
        Vector2 dir = screen - center;
        if (behind)
            dir = -dir;
        if (dir.LengthSquared() < 1f)
            dir = Vector2.Down;
        dir = dir.Normalized();

        // Punto sul bordo interno lungo la direzione del partner.
        float scaleX = dir.X != 0 ? (Size.X * 0.5f - Margin) / Mathf.Abs(dir.X) : float.MaxValue;
        float scaleY = dir.Y != 0 ? (Size.Y * 0.5f - Margin) / Mathf.Abs(dir.Y) : float.MaxValue;
        Vector2 point = center + dir * Mathf.Min(scaleX, scaleY);

        Vector2 side = new(-dir.Y, dir.X);
        DrawCircle(point, 11f, color with { A = 0.85f });
        DrawColoredPolygon(
            [point + dir * 24f, point + side * 9f, point - side * 9f],
            color with { A = 0.85f });
    }
}
