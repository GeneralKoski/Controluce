using Controluce.Core;
using Godot;

namespace Controluce.Player;

// Segnale "guarda qui": sfera pulsante visibile a entrambi i player, si dissolve da sola.
public partial class PingMarker : Node3D
{
    public Color Color { get; set; } = Colors.White;

    private const float Lifetime = 2f;
    private MeshInstance3D _sphere = null!;
    private StandardMaterial3D _material = null!;
    private float _age;

    public override void _Ready()
    {
        _material = new StandardMaterial3D
        {
            AlbedoColor = Color,
            EmissionEnabled = true,
            Emission = Color,
            EmissionEnergyMultiplier = 2f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        _sphere = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.35f, Height = 0.7f },
            MaterialOverride = _material,
            Layers = PhaseLayers.RenderNeutral,
        };
        AddChild(_sphere);

        var audio = new AudioStreamPlayer3D
        {
            Stream = AudioSynth.Tone(880f, 0.18f),
            Autoplay = true,
            VolumeDb = -8f,
        };
        AddChild(audio);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_age >= Lifetime)
        {
            QueueFree();
            return;
        }

        float progress = _age / Lifetime;
        float pulse = 1f + 0.35f * Mathf.Sin(_age * 12f);
        _sphere.Scale = Vector3.One * pulse;
        _material.AlbedoColor = new Color(Color.R, Color.G, Color.B, 1f - progress);
        Position += Vector3.Up * (float)delta * 0.4f;
    }
}
