using Godot;

namespace Controluce.Core;

// Progressi di gioco (user://save.cfg): ultima stanza raggiunta.
public static class Progress
{
    private const string FilePath = "user://save.cfg";
    private static bool _loaded;

    public static int LastRoom { get; private set; }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;

        var file = new ConfigFile();
        if (file.Load(FilePath) == Error.Ok)
            LastRoom = (int)file.GetValue("progress", "last_room", 0);
    }

    public static void RecordRoom(int index)
    {
        Load();
        if (index <= LastRoom)
            return;

        LastRoom = index;
        var file = new ConfigFile();
        file.SetValue("progress", "last_room", LastRoom);
        file.Save(FilePath);
    }
}
