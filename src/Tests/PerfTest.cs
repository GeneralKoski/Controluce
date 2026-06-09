using System.Diagnostics;
using Controluce.Core;
using Godot;

namespace Controluce.Tests;

// Harness di profiling (non headless): carica main.tscn, misura draw call,
// tempi di frame, tempi di caricamento stanze e ricostruzioni dei blocchi.
// Uso: godot-mono --path . scenes/tests/test_perf.tscn
public partial class PerfTest : Node
{
    private GameManager _game = null!;
    private int _frames;
    private double _frameTimeSum;
    private int _frameTimeSamples;
    private long _rebuildsAtRoomStart;

    public override void _Ready()
    {
        AddChild(GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate());
        _game = GetNode<GameManager>("Main");
    }

    public override void _Process(double delta)
    {
        _frames++;

        // Stanza 1: regime statico.
        if (_frames > 30 && _frames <= 150)
        {
            _frameTimeSum += delta;
            _frameTimeSamples++;
        }

        if (_frames == 150)
        {
            Report("room_01");
            var sw = Stopwatch.StartNew();
            _game.LoadRoom(1);
            GD.Print($"LoadRoom(room_02): {sw.ElapsedMilliseconds} ms");
            _rebuildsAtRoomStart = Level.PhaseGeometry.BuildCount;
            _frameTimeSum = 0;
            _frameTimeSamples = 0;
        }

        // Stanza 2: contiene blocchi a fase alternante (rebuild a runtime).
        if (_frames > 180 && _frames <= 480)
        {
            _frameTimeSum += delta;
            _frameTimeSamples++;
        }

        if (_frames == 480)
        {
            Report("room_02 (5 s, blocchi alternanti)");
            GD.Print($"Rebuild blocchi in 5 s su room_02: {Level.PhaseGeometry.BuildCount - _rebuildsAtRoomStart}");

            var sw = Stopwatch.StartNew();
            _game.LoadRoom(2);
            GD.Print($"LoadRoom(room_03): {sw.ElapsedMilliseconds} ms");

            GD.Print("PERF TEST: PASS");
            GetTree().Quit();
        }
    }

    private void Report(string label)
    {
        double avgMs = _frameTimeSamples > 0 ? _frameTimeSum / _frameTimeSamples * 1000.0 : 0;
        long drawCalls = (long)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame);
        long objects = (long)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame);
        long primitives = (long)RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame);
        GD.Print($"[{label}] frame medio: {avgMs:F2} ms | draw call: {drawCalls} | oggetti: {objects} | primitive: {primitives}");
    }
}
