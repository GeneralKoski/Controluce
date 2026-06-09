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

# Avvia il gioco (split-screen locale)
godot-mono --path .
```

## Online co-op (server authoritative)

La simulazione gira solo sul server (che è anche il Player 1); il client invia
i comandi e renderizza lo stato ricevuto, giocando come Player 2 con i
controlli di P1 (WASD ecc.).

```bash
# Host (gioca come P1)
godot-mono --path . -- --server 7777

# Ospite (gioca come P2)
godot-mono --path . -- --client <ip-host> 7777
```

In alternativa: variabili d'ambiente `CONTROLUCE_MODE` (server/client),
`CONTROLUCE_HOST`, `CONTROLUCE_PORT`. Nessuna configurazione nel sorgente.

## Stato

- [x] M0 — Setup progetto
- [x] M1 — Player controller base
- [x] M2 — Split-screen 2 player locali
- [x] M3 — Sistema fasi (blu/rosso)
- [x] M4 — Vista asimmetrica
- [x] M5 — La corda
- [x] M6 — Meccanica contrappeso
- [x] M7 — Prima stanza puzzle
- [x] M8 — Checkpoint & respawn
- [x] M9 — Audio & feedback
- [x] M10 — Tooling level design
- [x] M11 — Refactor authoritative
- [x] M12 — Online co-op (server authoritative)

Extra oltre le milestone: progressione su 3 stanze con transizioni, pedane a
peso, piattaforme mobili/porte/ascensori, blocchi a fase alternante, tira-corda
con feedback, dondolio controllabile, menu pausa, musica ambient.

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
| Movimento (relativo alla camera) | WASD | Stick sinistro gamepad / Frecce |
| Ruota camera | Mouse | Stick destro gamepad |
| Salto | Spazio | Tasto A gamepad / Invio |
| Ping "guarda qui" | E | Tasto B gamepad / Shift |
| Tira la corda (tieni premuto) | Q | Tasto X gamepad / Ctrl |
| Dondola (da appeso) | A/D nel verso del moto | Stick nel verso del moto |
| Pausa | Esc | Start |

## Test

Test headless (escono con codice 0/1 e stampano PASS/FAIL):

```bash
godot-mono --headless --path . scenes/tests/test_phases.tscn
godot-mono --headless --path . scenes/tests/test_rope.tscn
godot-mono --headless --path . scenes/tests/test_counterweight.tscn
godot-mono --headless --path . scenes/tests/test_exitzone.tscn
godot-mono --headless --path . scenes/tests/test_checkpoint.tscn
godot-mono --headless --path . scenes/tests/test_pull.tscn
godot-mono --headless --path . scenes/tests/test_swing.tscn
godot-mono --headless --path . scenes/tests/test_mechanics.tscn
godot-mono --headless --path . scenes/tests/test_progression.tscn
```

Test di rete (due processi su loopback):

```bash
CONTROLUCE_MODE=server CONTROLUCE_PORT=39555 godot-mono --headless --path . scenes/tests/test_net_server.tscn &
sleep 4
CONTROLUCE_MODE=client CONTROLUCE_PORT=39555 godot-mono --headless --path . scenes/tests/test_net_client.tscn
```
