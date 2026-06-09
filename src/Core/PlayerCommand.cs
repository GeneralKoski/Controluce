using Godot;

namespace Controluce.Core;

// Intent di un player per un tick di simulazione. È l'unico canale tra
// input e simulazione: in locale lo produce PlayerInput, online arriverà dalla rete.
public readonly record struct PlayerCommand(Vector2 MoveAxis, bool JumpPressed, bool PingPressed);
