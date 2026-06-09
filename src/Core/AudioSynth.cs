using System;
using Godot;

namespace Controluce.Core;

// Suoni placeholder generati proceduralmente: niente asset binari nel repo.
public static class AudioSynth
{
    private const int MixRate = 22050;

    public static AudioStreamWav Tone(float frequency, float seconds, float gain = 0.35f)
    {
        int count = (int)(MixRate * seconds);
        var data = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)MixRate;
            float envelope = 1f - i / (float)count;
            WriteSample(data, i, Mathf.Sin(Mathf.Tau * frequency * t) * envelope * gain);
        }
        return Wrap(data);
    }

    public static AudioStreamWav Chime(float gain = 0.3f)
    {
        var first = ToneSamples(523f, 0.12f, gain);
        var second = ToneSamples(784f, 0.25f, gain);
        var data = new byte[(first.Length + second.Length)];
        first.CopyTo(data, 0);
        second.CopyTo(data, first.Length);
        return Wrap(data);
    }

    public static AudioStreamWav NoiseBurst(float seconds, float gain = 0.25f)
    {
        var rng = new Random(42);
        int count = (int)(MixRate * seconds);
        var data = new byte[count * 2];
        float smoothed = 0f;
        for (int i = 0; i < count; i++)
        {
            float envelope = 1f - i / (float)count;
            // Media mobile = rumore "morbido", da passo su pietra.
            smoothed = smoothed * 0.6f + ((float)rng.NextDouble() * 2f - 1f) * 0.4f;
            WriteSample(data, i, smoothed * envelope * envelope * gain);
        }
        return Wrap(data);
    }

    public static AudioStreamWav Creak(float gain = 0.35f)
    {
        var rng = new Random(7);
        const float seconds = 0.35f;
        int count = (int)(MixRate * seconds);
        var data = new byte[count * 2];
        float smoothed = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)MixRate;
            float progress = i / (float)count;
            float envelope = Mathf.Sin(progress * Mathf.Pi);
            float frequency = 160f - 60f * progress;
            smoothed = smoothed * 0.7f + ((float)rng.NextDouble() * 2f - 1f) * 0.3f;
            float sample = (Mathf.Sin(Mathf.Tau * frequency * t) * 0.6f + smoothed * 0.4f) * envelope * gain;
            WriteSample(data, i, sample);
        }
        return Wrap(data);
    }

    // Pad ambient in loop: tutte le frequenze (voci e LFO) hanno cicli interi
    // sugli 8 secondi, così il loop è senza cuciture.
    public static AudioStreamWav AmbientPad(float gain = 0.16f)
    {
        const float seconds = 8f;
        int count = (int)(MixRate * seconds);
        var data = new byte[count * 2];
        float[] voices = [110f, 165f, 220f, 247.5f];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)MixRate;
            float sample = 0f;
            for (int v = 0; v < voices.Length; v++)
            {
                float lfo = 0.5f + 0.5f * Mathf.Sin(Mathf.Tau * (0.125f * (v + 1)) * t + v * 1.7f);
                sample += Mathf.Sin(Mathf.Tau * voices[v] * t) * lfo;
            }
            WriteSample(data, i, sample / voices.Length * gain);
        }

        var wav = Wrap(data);
        wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        wav.LoopEnd = count;
        return wav;
    }

    private static byte[] ToneSamples(float frequency, float seconds, float gain)
    {
        int count = (int)(MixRate * seconds);
        var data = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)MixRate;
            float envelope = 1f - i / (float)count;
            WriteSample(data, i, Mathf.Sin(Mathf.Tau * frequency * t) * envelope * gain);
        }
        return data;
    }

    private static void WriteSample(byte[] data, int index, float value)
    {
        short sample = (short)(Mathf.Clamp(value, -1f, 1f) * short.MaxValue);
        data[index * 2] = (byte)(sample & 0xFF);
        data[index * 2 + 1] = (byte)((sample >> 8) & 0xFF);
    }

    private static AudioStreamWav Wrap(byte[] data) => new()
    {
        Data = data,
        Format = AudioStreamWav.FormatEnum.Format16Bits,
        MixRate = MixRate,
        Stereo = false,
    };
}
