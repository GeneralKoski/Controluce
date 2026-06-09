using Godot;

namespace Controluce.Core;

public partial class PauseMenu : Control
{
    [Export] public GameManager? Game { get; set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        GetNode<Button>("Panel/VBox/Resume").Pressed += TogglePause;
        GetNode<Button>("Panel/VBox/Restart").Pressed += OnRestart;
        GetNode<Button>("Panel/VBox/Quit").Pressed += () => GetTree().Quit();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Online niente pausa: il server non si ferma per un client in pausa.
        if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer)
            return;

        if (@event.IsActionPressed("pause"))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }

    private void TogglePause()
    {
        bool paused = !GetTree().Paused;
        GetTree().Paused = paused;
        Visible = paused;
    }

    private void OnRestart()
    {
        Game?.RestartRoom();
        TogglePause();
    }
}
