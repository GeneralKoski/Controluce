using Godot;

namespace Controluce.Core;

// Palette per stanza: la progressione alba → notte racconta il viaggio.
// Il sole resta basso davanti ai player (-Z, controluce), ma colore del
// cielo, tinta della luce, nebbia ed elevazione cambiano stanza per stanza.
public static class RoomMood
{
    private readonly struct Mood(
        Color skyTop, Color skyHorizon, Color sun, float sunEnergy,
        float sunElevationDeg, Color fog, float fogDensity, Color fill,
        float ambientEnergy)
    {
        public readonly Color SkyTop = skyTop;
        public readonly Color SkyHorizon = skyHorizon;
        public readonly Color Sun = sun;
        public readonly float SunEnergy = sunEnergy;
        public readonly float SunElevationDeg = sunElevationDeg;
        public readonly Color Fog = fog;
        public readonly float FogDensity = fogDensity;
        public readonly Color Fill = fill;
        public readonly float AmbientEnergy = ambientEnergy;
    }

    private static readonly Mood[] Moods =
    [
        // Stanza 1 — alba: cielo rosato, luce dorata tenue, aria pulita.
        new(new Color(0.10f, 0.12f, 0.22f), new Color(0.98f, 0.62f, 0.38f),
            new Color(1f, 0.84f, 0.62f), 1.2f, 13f,
            new Color(0.50f, 0.38f, 0.36f), 0.005f,
            new Color(0.50f, 0.58f, 0.85f), 2.3f),
        // Stanza 2 — mattino: orizzonte ambrato, sole più alto e deciso.
        new(new Color(0.08f, 0.11f, 0.20f), new Color(0.95f, 0.52f, 0.26f),
            new Color(1f, 0.78f, 0.55f), 1.3f, 15f,
            new Color(0.45f, 0.32f, 0.32f), 0.006f,
            new Color(0.45f, 0.55f, 0.85f), 2.2f),
        // Stanza 3 — tramonto: arancio acceso, controluce al massimo.
        new(new Color(0.07f, 0.06f, 0.16f), new Color(1f, 0.42f, 0.18f),
            new Color(1f, 0.62f, 0.38f), 1.45f, 9f,
            new Color(0.46f, 0.26f, 0.26f), 0.008f,
            new Color(0.42f, 0.45f, 0.80f), 2.1f),
        // Stanza 4 — crepuscolo: viola e magenta, sole sull'orizzonte.
        new(new Color(0.05f, 0.04f, 0.14f), new Color(0.78f, 0.30f, 0.34f),
            new Color(0.95f, 0.50f, 0.45f), 1.1f, 5f,
            new Color(0.35f, 0.22f, 0.32f), 0.010f,
            new Color(0.40f, 0.38f, 0.78f), 1.9f),
        // Stanza 5 — notte: blu profondo, ultimo bagliore freddo, fog densa.
        new(new Color(0.02f, 0.03f, 0.09f), new Color(0.30f, 0.22f, 0.38f),
            new Color(0.70f, 0.62f, 0.78f), 0.85f, 3f,
            new Color(0.16f, 0.16f, 0.28f), 0.013f,
            new Color(0.35f, 0.40f, 0.75f), 1.7f),
        // Stanza 6 — prima dell'alba: buio freddo, l'orizzonte si rischiara.
        new(new Color(0.015f, 0.02f, 0.07f), new Color(0.22f, 0.28f, 0.46f),
            new Color(0.62f, 0.68f, 0.92f), 0.8f, 2f,
            new Color(0.12f, 0.14f, 0.24f), 0.014f,
            new Color(0.38f, 0.44f, 0.80f), 1.6f),
    ];

    public static void Apply(Node3D world, int roomIndex)
    {
        Mood mood = Moods[Mathf.Clamp(roomIndex, 0, Moods.Length - 1)];

        if (world.GetNodeOrNull<WorldEnvironment>("WorldEnvironment")?.Environment is { } env)
        {
            env.AmbientLightEnergy = mood.AmbientEnergy;
            env.FogLightColor = mood.Fog;
            env.FogDensity = mood.FogDensity;
            if (env.Sky?.SkyMaterial is ProceduralSkyMaterial sky)
            {
                sky.SkyTopColor = mood.SkyTop;
                sky.SkyHorizonColor = mood.SkyHorizon;
                sky.GroundHorizonColor = mood.SkyHorizon * 0.92f;
                sky.GroundBottomColor = mood.SkyTop * 1.1f;
            }
        }

        if (world.GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D") is { } sun)
        {
            sun.LightColor = mood.Sun;
            sun.LightEnergy = mood.SunEnergy;
            // Sole davanti ai player (-Z), elevazione del mood.
            sun.Rotation = new Vector3(
                Mathf.DegToRad(mood.SunElevationDeg - 180f), 0f, Mathf.Pi);
        }

        if (world.GetNodeOrNull<DirectionalLight3D>("FillLight") is { } fill)
            fill.LightColor = mood.Fill;
    }
}
