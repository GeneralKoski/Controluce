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
- [ ] M1 — Player controller base
- [ ] M2 — Split-screen 2 player locali
- [ ] M3 — Sistema fasi (blu/rosso)
- [ ] M4 — Vista asimmetrica
- [ ] M5 — La corda
- [ ] M6 — Meccanica contrappeso
- [ ] M7 — Prima stanza puzzle
- [ ] M8 — Checkpoint & respawn
- [ ] M9 — Audio & feedback
- [ ] M10 — Tooling level design
- [ ] M11 — Refactor authoritative

## Controlli

_(in arrivo con M1/M2)_
