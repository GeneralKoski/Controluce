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

## Controlli

| Azione | Player 1 | Player 2 |
| --- | --- | --- |
| Movimento | WASD | Stick sinistro gamepad / Frecce |
| Salto | Spazio | Tasto A gamepad / Invio |
