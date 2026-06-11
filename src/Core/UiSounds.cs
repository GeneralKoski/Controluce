using Godot;

namespace Controluce.Core;

// Suoni di interfaccia procedurali: un tick sull'hover/focus e una nota
// sul click, agganciati a tutti i bottoni sotto una radice.
public static class UiSounds
{
    public static void Attach(Node root)
    {
        var hover = new AudioStreamPlayer { Stream = AudioSynth.Tone(740f, 0.04f), VolumeDb = -16f };
        var press = new AudioStreamPlayer { Stream = AudioSynth.Tone(523f, 0.09f), VolumeDb = -10f };
        root.AddChild(hover);
        root.AddChild(press);

        foreach (Node node in root.FindChildren("*", "BaseButton", recursive: true, owned: false))
        {
            if (node is not BaseButton button)
                continue;

            button.MouseEntered += () => hover.Play();
            button.FocusEntered += () => hover.Play();
            button.Pressed += () => press.Play();
        }
    }
}
