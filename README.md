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

# Avvia il gioco (parte dal menu principale)
godot-mono --path .
```

Dal menu: **Gioca** (split-screen), **Continua** (riprende dall'ultima stanza
raggiunta, salvata in `user://save.cfg`), **Online** (ospita o unisciti via
IP/porta), **Personaggi** (skin procedurali separate per P1 e P2 con anteprima 3D
e scambio ruoli blu/rosso), **Opzioni** (volume, sensibilità mouse/stick, schermo intero,
modalità respawn) — tutto salvato in `user://settings.cfg`.

Online l'aspetto è concordato: l'host detta la propria skin P1 e lo scambio
ruoli, l'ospite porta la propria skin P2.

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

Extra oltre le milestone: progressione su 6 stanze con transizioni (la
quarta, "Il pendolo", è costruita su dondolio e salto da appeso; la quinta,
"La porta sul vuoto", inverte i ruoli e aggiunge pedana-porta e zattera
mobile; la sesta, "Il traghettatore", chiude con ascensore a pedana,
discesa sincronizzata su percorsi asimmetrici e pedana a due giocatori
con porta a tempo), pedane a
peso, piattaforme mobili/porte/ascensori, blocchi a fase alternante, tira-corda
con feedback, dondolio controllabile e salto da appeso, coyote time + jump
buffer, camera orbitale con occlusione, menu principale con lobby online,
opzioni e salvataggio progressi, menu pausa, musica ambient, occhi/particelle
sui player e griglia procedurale, ambiente "controluce" (sole basso, glow,
SSAO, nebbia, ombre morbide) con blocchi emissivi e geometria fantasma
fresnel, palette che racconta il viaggio (alba → mattino → tramonto →
crepuscolo → notte lungo le cinque stanze), skin procedurali con scambio
ruoli, indicatore del partner fuori vista (freccia sul bordo, sagoma
"a raggi X" dietro i muri), schermata di finale al completamento
dell'ultima stanza (anche per l'ospite online).

## Export

Preset desktop pronti in `export_presets.cfg` (macOS universal, Windows
x86_64, Linux x86_64). Servono i template di export di Godot 4.6
(Editor → Manage Export Templates), poi:

```bash
mkdir -p build
godot-mono --headless --path . --export-release "macOS" build/Controluce.zip
```

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
| Salto (anche da appeso, con coyote time e buffer) | Spazio | Tasto A gamepad / Invio |
| Ping "guarda qui" | E | Tasto B gamepad / Shift |
| Tira la corda (tieni premuto) | Q | Tasto X gamepad / Ctrl |
| Dondola (da appeso) | A/D nel verso del moto | Stick nel verso del moto |
| Pausa | Esc | Start |

## Test

Test headless (escono con codice 0/1 e stampano PASS/FAIL). Tutta la suite,
test di rete inclusi, con:

```bash
./run_tests.sh
```

Oppure singolarmente:

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
godot-mono --headless --path . scenes/tests/test_skins.tscn
godot-mono --headless --path . scenes/tests/test_room04.tscn
godot-mono --headless --path . scenes/tests/test_room05.tscn
godot-mono --headless --path . scenes/tests/test_room06.tscn
```

Test di rete (due processi su loopback):

```bash
CONTROLUCE_MODE=server CONTROLUCE_PORT=39555 godot-mono --headless --path . scenes/tests/test_net_server.tscn &
sleep 4
CONTROLUCE_MODE=client CONTROLUCE_PORT=39555 godot-mono --headless --path . scenes/tests/test_net_client.tscn
```
