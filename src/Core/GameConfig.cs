namespace Controluce.Core;

// Parametri di lancio della partita scelti dal menu (statici: sopravvivono
// al cambio scena). Per gli avvii da CLI restano i default e decide
// NetworkManager via argomenti/variabili d'ambiente.
public static class GameConfig
{
    public static string? Mode { get; set; } // "server" | "client" | null = locale
    public static string Host { get; set; } = "127.0.0.1";
    public static int Port { get; set; } = 7777;
    public static int StartRoom { get; set; }
}
