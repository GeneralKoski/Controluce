namespace Controluce.Core;

public enum Phase
{
    Blue,
    Red,
}

public static class PhaseLayers
{
    // Collision layers (bit flags)
    public const uint Neutral = 1 << 0;
    public const uint BlueGeometry = 1 << 1;
    public const uint RedGeometry = 1 << 2;
    public const uint Player = 1 << 3;

    // Render layers (VisualInstance3D.Layers / Camera3D.CullMask)
    public const uint RenderNeutral = 1 << 0;
    public const uint RenderBlueSolid = 1 << 1;
    public const uint RenderRedSolid = 1 << 2;
    public const uint RenderBlueGhost = 1 << 3;
    public const uint RenderRedGhost = 1 << 4;

    public static uint GeometryLayerFor(Phase phase) =>
        phase == Phase.Blue ? BlueGeometry : RedGeometry;

    public static uint PlayerCollisionMaskFor(Phase phase) =>
        Neutral | Player | GeometryLayerFor(phase);

    public static uint SolidRenderLayerFor(Phase phase) =>
        phase == Phase.Blue ? RenderBlueSolid : RenderRedSolid;

    public static uint GhostRenderLayerFor(Phase phase) =>
        phase == Phase.Blue ? RenderBlueGhost : RenderRedGhost;

    // La camera di un player vede: geometria neutra, la propria fase solida,
    // il "fantasma" della fase opposta e gli altri oggetti neutri (player inclusi).
    public static uint CameraCullMaskFor(Phase phase) =>
        RenderNeutral | SolidRenderLayerFor(phase) | GhostRenderLayerFor(Opposite(phase));

    public static Phase Opposite(Phase phase) =>
        phase == Phase.Blue ? Phase.Red : Phase.Blue;
}
