using Godot;

namespace Controluce.Core;

// Impostazioni persistenti (user://settings.cfg).
public static class Settings
{
    private const string FilePath = "user://settings.cfg";
    private static bool _loaded;

    public static float MasterVolume { get; set; } = 0.8f;
    public static float MouseSensitivity { get; set; } = 0.003f;
    public static float StickSpeed { get; set; } = 2.5f;
    public static bool RespawnBoth { get; set; } = true;
    public static bool Fullscreen { get; set; } = true;
    public static int SkinP1 { get; set; }
    public static int SkinP2 { get; set; }
    public static bool SwapRoles { get; set; }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;

        var file = new ConfigFile();
        if (file.Load(FilePath) == Error.Ok)
        {
            MasterVolume = (float)file.GetValue("audio", "master", 0.8f);
            MouseSensitivity = (float)file.GetValue("input", "mouse_sensitivity", 0.003f);
            StickSpeed = (float)file.GetValue("input", "stick_speed", 2.5f);
            RespawnBoth = (bool)file.GetValue("game", "respawn_both", true);
            Fullscreen = (bool)file.GetValue("video", "fullscreen", true);
            SkinP1 = (int)file.GetValue("skins", "p1", 0);
            SkinP2 = (int)file.GetValue("skins", "p2", 0);
            SwapRoles = (bool)file.GetValue("game", "swap_roles", false);
        }
        ApplyVolume();
    }

    public static void Save()
    {
        var file = new ConfigFile();
        file.SetValue("audio", "master", MasterVolume);
        file.SetValue("input", "mouse_sensitivity", MouseSensitivity);
        file.SetValue("input", "stick_speed", StickSpeed);
        file.SetValue("game", "respawn_both", RespawnBoth);
        file.SetValue("video", "fullscreen", Fullscreen);
        file.SetValue("skins", "p1", SkinP1);
        file.SetValue("skins", "p2", SkinP2);
        file.SetValue("game", "swap_roles", SwapRoles);
        file.Save(FilePath);
    }

    public static void ApplyVolume() =>
        AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Mathf.Max(MasterVolume, 0.001f)));

    public static void ApplyWindowMode() =>
        DisplayServer.WindowSetMode(Fullscreen
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed);
}
