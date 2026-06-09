# Controluce

Co-op puzzle-platformer 3D anti-rage. Due giocatori, due percezioni diverse della stessa stanza, una corda che li lega. Nessuno finisce un livello da solo.

## Requisiti

- [Godot 4.6.x .NET](https://godotengine.org/) (`brew install --cask godot-mono`)
- [.NET SDK 8+](https://dotnet.microsoft.com/) (`brew install dotnet`)

## Avvio

```bash
# Build C#
dotnet build

# Apri l'editor
godot-mono --editor --path .

# Avvia il gioco
godot-mono --path .
```

## Stato

- [x] M0 — Setup progetto
- [x] M1 — Player controller base
- [x] M2 — Split-screen 2 player locali
- [x] M3 — Sistema fasi (blu/rosso)
- [x] M4 — Vista asimmetrica
- [x] M5 — La corda
- [x] M6 — Meccanica contrappeso
- [x] M7 — Prima stanza puzzle
- [ ] M8 — Checkpoint & respawn
- [ ] M9 — Audio & feedback
- [ ] M10 — Tooling level design
- [ ] M11 — Refactor authoritative

## Level design

Per prototipare una stanza nuova in pochi minuti:

1. Duplica `scenes/rooms/room_template.tscn` e rinominala (`room_02.tscn`, ...).
2. Componi la geometria con nodi `StaticBody3D` + script `PhaseBlock`:
   - `BlockPhase`: `Blue` (solo P1), `Red` (solo P2), `Neutral` (entrambi);
   - `Size`: dimensioni del box (collisione e mesh si aggiornano da sole);
   - la posizione fa snap su griglia da 0.25 m (disattivabile con `SnapToGrid`).
3. Tieni i due percorsi entro la lunghezza della corda (6 m di default).
4. Aggiungi `Checkpoint` (con i due Marker3D `SpawnA`/`SpawnB`) dopo ogni passaggio rischioso.
5. L'`ExitZone` scatta solo quando entrambi i player sono dentro.
6. Per provarla: in `scenes/main.tscn` sostituisci l'istanza `Room01` con la tua stanza.

## Controlli

| Azione | Player 1 | Player 2 |
| --- | --- | --- |
| Movimento | WASD | Stick sinistro gamepad / Frecce |
| Salto | Spazio | Tasto A gamepad / Invio |
