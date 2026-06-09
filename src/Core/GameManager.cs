using Godot;

namespace Controluce.Core;

public partial class GameManager : Node
{
    public override void _Ready()
    {
        // Il mondo 3D vive nel viewport root ed è condiviso dai due SubViewport:
        // il root non deve renderizzarlo (lo coprono i due schermi).
        GetViewport().Disable3D = true;
    }
}
