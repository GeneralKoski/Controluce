using Controluce.Core;
using Godot;

namespace Controluce.Level;

[Tool]
public partial class PhaseBlock : StaticBody3D, IActivatable
{
    private Phase _phase = Phase.Blue;
    private Vector3 _size = new(2f, 0.5f, 2f);
    private bool _snapping;
    private MeshInstance3D? _solidMesh;
    private Phase _basePhase;
    private bool _baseCaptured;
    private float _toggleTimer;

    // Snap della posizione su griglia (solo in editor), per comporre stanze in fretta.
    [Export] public bool SnapToGrid { get; set; } = true;
    [Export] public float GridStep { get; set; } = 0.25f;

    // Fase alternante: ogni TogglePeriod secondi il blocco passa Blu<->Rosso
    // (0 = statico). ToggleOffset sfasa il timer per creare pattern.
    [Export] public float TogglePeriod { get; set; }
    [Export] public float ToggleOffset { get; set; }

    [Export]
    public Phase BlockPhase
    {
        get => _phase;
        set { _phase = value; if (IsInsideTree()) Rebuild(); }
    }

    [Export]
    public Vector3 Size
    {
        get => _size;
        set { _size = value; if (IsInsideTree()) Rebuild(); }
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            SetNotifyLocalTransform(true);

        if (!_baseCaptured)
        {
            _basePhase = _phase;
            _baseCaptured = true;
            _toggleTimer = ToggleOffset;
        }
        Rebuild();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || TogglePeriod <= 0f || _phase == Phase.Neutral)
            return;

        _toggleTimer += (float)delta;
        if (_toggleTimer >= TogglePeriod)
        {
            _toggleTimer -= TogglePeriod;
            BlockPhase = PhaseLayers.Opposite(_phase);
            return;
        }

        // Preavviso: il blocco "lampeggia" prima di cambiare fase.
        float remaining = TogglePeriod - _toggleTimer;
        if (_solidMesh != null)
            _solidMesh.Transparency = remaining < 0.6f ? 0.3f + 0.3f * Mathf.Sin(_toggleTimer * 25f) : 0f;
    }

    // Attivata da una WeightPlate: fase opposta finché è attiva.
    public void SetActivated(bool active)
    {
        if (_basePhase == Phase.Neutral)
            return;
        BlockPhase = active ? PhaseLayers.Opposite(_basePhase) : _basePhase;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationLocalTransformChanged
            || !Engine.IsEditorHint() || !SnapToGrid || _snapping || GridStep <= 0f)
            return;

        _snapping = true;
        Position = (Position / GridStep).Round() * GridStep;
        _snapping = false;
    }

    private void Rebuild() => _solidMesh = PhaseGeometry.Build(this, _phase, _size);
}
