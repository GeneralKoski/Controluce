using Godot;

namespace Controluce.Core;

public partial class MainMenu : Control
{
    private const string GameScene = "res://scenes/main.tscn";

    private Control _home = null!;
    private Control _online = null!;
    private Control _options = null!;
    private Control _skins = null!;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Settings.Load();
        Settings.ApplyWindowMode();
        Progress.Load();

        _home = GetNode<Control>("Home");
        _online = GetNode<Control>("Online");
        _options = GetNode<Control>("Options");
        _skins = GetNode<Control>("Skins");

        var continueButton = GetNode<Button>("Home/Continua");
        continueButton.Disabled = Progress.LastRoom <= 0;
        continueButton.Text = Progress.LastRoom > 0
            ? $"Continua (stanza {Progress.LastRoom + 1})"
            : "Continua";

        GetNode<Button>("Home/Gioca").Pressed += () => StartGame(null, 0);
        continueButton.Pressed += () => StartGame(null, Progress.LastRoom);
        GetNode<Button>("Home/Online").Pressed += () => ShowPanel(_online);
        GetNode<Button>("Home/Personaggi").Pressed += () => ShowPanel(_skins);
        GetNode<Button>("Home/Opzioni").Pressed += () => ShowPanel(_options);
        GetNode<Button>("Home/Esci").Pressed += () => GetTree().Quit();

        GetNode<Button>("Online/Ospita").Pressed += () => StartGame("server", 0);
        GetNode<Button>("Online/Unisciti").Pressed += () => StartGame("client", 0);
        GetNode<Button>("Online/Indietro").Pressed += () => ShowPanel(_home);

        SetupOptions();
        GetNode<Button>("Options/Indietro").Pressed += () =>
        {
            Settings.Save();
            ShowPanel(_home);
        };

        SetupSkins();
        GetNode<Button>("Skins/Indietro").Pressed += () =>
        {
            Settings.Save();
            ShowPanel(_home);
        };
    }

    private void StartGame(string? mode, int startRoom)
    {
        GameConfig.Mode = mode;
        GameConfig.StartRoom = startRoom;

        if (mode != null)
        {
            string host = GetNode<LineEdit>("Online/Host").Text.Trim();
            if (host.Length > 0)
                GameConfig.Host = host;
            if (int.TryParse(GetNode<LineEdit>("Online/Porta").Text.Trim(), out int port))
                GameConfig.Port = port;
        }

        GetTree().ChangeSceneToFile(GameScene);
    }

    private void ShowPanel(Control panel)
    {
        _home.Visible = panel == _home;
        _online.Visible = panel == _online;
        _options.Visible = panel == _options;
        _skins.Visible = panel == _skins;
    }

    private void SetupSkins()
    {
        var p1 = GetNode<OptionButton>("Skins/P1Row/P1Skin");
        var p2 = GetNode<OptionButton>("Skins/P2Row/P2Skin");
        foreach (string name in Player.PlayerSkin.Names)
        {
            p1.AddItem(name);
            p2.AddItem(name);
        }
        p1.Selected = Mathf.Clamp(Settings.SkinP1, 0, Player.PlayerSkin.Count - 1);
        p2.Selected = Mathf.Clamp(Settings.SkinP2, 0, Player.PlayerSkin.Count - 1);

        // Anteprime 3D affiancate, subito sotto il titolo del pannello.
        var previews = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        previews.AddThemeConstantOverride("separation", 40);
        var previewP1 = new SkinPreview();
        var previewP2 = new SkinPreview();
        previews.AddChild(previewP1);
        previews.AddChild(previewP2);
        var skinsPanel = GetNode<Control>("Skins");
        skinsPanel.AddChild(previews);
        skinsPanel.MoveChild(previews, 1);

        void RefreshPreviews()
        {
            Phase phase1 = Settings.SwapRoles ? Phase.Red : Phase.Blue;
            previewP1.ShowSkin(phase1, Settings.SkinP1);
            previewP2.ShowSkin(PhaseLayers.Opposite(phase1), Settings.SkinP2);
        }

        p1.ItemSelected += index =>
        {
            Settings.SkinP1 = (int)index;
            RefreshPreviews();
        };
        p2.ItemSelected += index =>
        {
            Settings.SkinP2 = (int)index;
            RefreshPreviews();
        };

        var swap = GetNode<CheckBox>("Skins/SwapRoles");
        swap.ButtonPressed = Settings.SwapRoles;
        swap.Toggled += pressed =>
        {
            Settings.SwapRoles = pressed;
            RefreshPreviews();
        };

        RefreshPreviews();
    }

    private void SetupOptions()
    {
        var volume = GetNode<HSlider>("Options/VolumeRow/Volume");
        volume.Value = Settings.MasterVolume * 100.0;
        volume.ValueChanged += value =>
        {
            Settings.MasterVolume = (float)(value / 100.0);
            Settings.ApplyVolume();
        };

        var mouse = GetNode<HSlider>("Options/MouseRow/Mouse");
        mouse.Value = Settings.MouseSensitivity * 1000.0;
        mouse.ValueChanged += value => Settings.MouseSensitivity = (float)(value / 1000.0);

        var stick = GetNode<HSlider>("Options/StickRow/Stick");
        stick.Value = Settings.StickSpeed;
        stick.ValueChanged += value => Settings.StickSpeed = (float)value;

        var fullscreen = GetNode<CheckBox>("Options/Fullscreen");
        fullscreen.ButtonPressed = Settings.Fullscreen;
        fullscreen.Toggled += pressed =>
        {
            Settings.Fullscreen = pressed;
            Settings.ApplyWindowMode();
        };

        var respawn = GetNode<CheckBox>("Options/RespawnBoth");
        respawn.ButtonPressed = Settings.RespawnBoth;
        respawn.Toggled += pressed => Settings.RespawnBoth = pressed;
    }
}
