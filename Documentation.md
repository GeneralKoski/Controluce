# Controluce — INSTRUCTIONS.md

> Co-op puzzle-platformer 3D anti-rage. Due giocatori, due percezioni diverse della stessa stanza, una corda che li lega. Nessuno finisce un livello da solo.

Questo documento è la guida per Claude Code. Procedi  **per milestone incrementali** , una alla volta, e **fermati a fine milestone per conferma** prima di passare alla successiva. Non saltare avanti.

---

## 1. Concept

Stessa stanza, due  **fasi mirror** :

* Il **Player A** vede e calpesta solo la geometria  *blu* . La geometria *rossa* per lui è vuoto.
* Il **Player B** vede e calpesta solo la geometria  *rossa* . La *blu* per lui è vuoto.
* I due sono uniti da una **corda** con lunghezza massima: non possono allontanarsi oltre un limite, e il peso/tensione dell'uno è una meccanica per l'altro (B si appende dal suo lato per fare da contrappeso e far oscillare A).

Il loop di gioco è  **comunicazione + lettura dello spazio** , non reflex:

> A vede un baratro dove per B c'è una piattaforma. A deve descriverlo a B. B si posiziona per reggere A. Il fallimento è quasi sempre "non ci siamo capiti" o "abbiamo letto male", mai "non ho i riflessi".

 **Anti-rage by design** : checkpoint generosi, respawn istantaneo e indolore, nessuna penalità di tempo/punteggio, la difficoltà sta nel *capire* la stanza, non nell'eseguirla al millisecondo.

---

## 2. Stack

| Componente           | Scelta                                                                  | Note                                                                                                       |
| -------------------- | ----------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Engine               | **Godot 4.x**(ultima stabile)                                     | Verifica con `godot --version`la versione installata e adatta le API.                                    |
| Linguaggio           | **C# / .NET**                                                     | Versione .NET allineata a quella richiesta dalla build Godot installata.                                   |
| Fisica               | **Jolt**(default in Godot 4.4+)                                   | Joint/constraint built-in per la corda.                                                                    |
| Multiplayer (fase 2) | **High-level Multiplayer API**(ENet) +`MultiplayerSynchronizer` | Solo dopo che il single-sim locale è validato.                                                            |
| Target build         | **Desktop**(Windows/Linux/macOS)                                  | C#**non**esporta ancora su web in Godot 4 (limite noto del runtime .NET). Niente web export per ora. |

**Filosofia architetturale (vincolante):** una **sola simulazione fisica authoritative** fin da subito, anche nella versione locale split-screen. I due player sono input verso un unico mondo simulato. Questo rende la transizione locale → online (fase 2) quasi indolore: la stessa simulazione si sposta lato server e i client diventano renderer dello stato. **Non** simulare la fisica in modo indipendente per i due lati.

---

## 3. Struttura repo

```
controluce/
├── INSTRUCTIONS.md
├── README.md
├── .gitignore            # template Godot + .NET (bin/, obj/, .godot/, *.mono/)
├── project.godot
├── Controluce.csproj
├── src/
│   ├── Core/             # GameManager, PhaseSystem, RopeConstraint
│   ├── Player/           # PlayerController, PlayerInput, CameraRig
│   ├── Level/            # Room, Phase tagging, Checkpoint, Spawn
│   └── Net/              # (fase 2) Server, ClientState, Sync
├── scenes/
│   ├── main.tscn
│   ├── player.tscn
│   └── rooms/
└── assets/
```

Convenzioni:

* C# in `PascalCase` per classi/metodi/proprietà, `_camelCase` per campi privati.
* Un nodo = una responsabilità. Niente god-object.
* **Conventional Commits in italiano** (`feat:`, `fix:`, `refactor:`, `chore:`). Commit atomici per milestone/step.
* **Nessun segreto hardcoded.** Per la fase 2 (server/netcode), endpoint e chiavi vanno via variabili d'ambiente o file di config ignorato da git, mai nel sorgente.

---

## 4. Milestone

Ogni milestone è giocabile/verificabile. **Conferma prima di proseguire.**

### M0 — Setup

* Init progetto Godot 4 .NET, `.gitignore` corretto, repo git, primo commit.
* `README.md` con come avviare il progetto.
* Verifica che il build C# compili e l'editor apra una scena vuota.

### M1 — Player controller base (single)

* Un solo player 3D: movimento (WASD/stick), salto, gravità, `CharacterBody3D`.
* `CameraRig` in terza persona che segue il player.
* Niente fasi, niente corda: solo che si muova bene su una piattaforma di test.

### M2 — Split-screen 2 player locali

* Due `SubViewport` affiancati, due `CameraRig`, due set di input (P1 tastiera, P2 gamepad; mappa input configurabile).
* **Una sola scena/mondo** , due player come due nodi nella stessa simulazione (rispetta la regola single-sim).

### M3 — Sistema fasi (blu/rosso)

* Tag di fase su ogni pezzo di geometria (`Phase.Blue` / `Phase.Red`).
* Collision layer/mask così che A collida solo col blu, B solo col rosso.
* Test: A cade attraverso il rosso, B cade attraverso il blu, ciascuno sta in piedi sulla propria fase.

### M4 — Vista asimmetrica

* Il viewport di A renderizza (o evidenzia) la geometria blu come solida e la rossa come "fantasma"/invisibile; viceversa per B.
* Implementazione via cull mask / layer di rendering per camera.
* Test: i due schermi mostrano due versioni diverse della stessa stanza.

### M5 — La corda (cuore tecnico)

* Vincolo di **distanza massima** tra i due player (tether). Approcci in ordine di preferenza:
  1. Constraint di distanza logico (clamp sulla distanza + impulso di richiamo quando si supera la lunghezza max) — semplice e stabile.
  2. `Generic6DOFJoint3D` / joint Jolt tra i due corpi con limite lineare.
  3. Corda visiva (catena di segmenti / verlet)  **solo per il rendering** , con il vincolo logico sopra a governare la fisica.
* La corda non deve esplodere/divergere: testala con movimenti bruschi e ai limiti.
* Test: se uno si allontana troppo, viene richiamato; la corda è visibile tra i due.

### M6 — Meccanica contrappeso

* Il peso/tensione di un player influenza l'altro tramite la corda (B si lascia cadere → trascina/sostiene A).
* Tarare massa, elasticità e smorzamento per renderlo *leggibile* e divertente, non frustrante.

### M7 — Prima stanza puzzle (verticale di gioco)

* Una stanza completa risolvibile **solo** con cooperazione: A vede un percorso che B non vede e viceversa, e serve la corda/contrappeso per superare almeno un passaggio.
* Trigger di "stanza completata" quando entrambi raggiungono l'uscita.

### M8 — Checkpoint & respawn (anti-rage)

* Checkpoint frequenti, respawn istantaneo di entrambi all'ultimo checkpoint, nessuna penalità.
* Se uno cade, si valuta: respawn solo lui al checkpoint, o entrambi (scegliere ciò che è meno frustrante alla prova).

### M9 — Audio & feedback

* Feedback di tensione corda (suono/visivo quando è tesa), passi, conferma di checkpoint.
* UI minimale: indicatore di quanto è tesa la corda, eventuale ping/segnale per comunicare ("guarda qui").

### M10 — Tooling per level design

* Rendere veloce costruire stanze nuove: scene componibili, marcatori di fase, snap su griglia, scena-template di stanza.
* Obiettivo: poter prototipare una stanza in minuti per iterare sul game design.

### M11 — Refactor verso authoritative pulito *(prep fase online)*

* Isolare nettamente: **simulazione** (autorità sullo stato) vs **input** vs  **rendering** .
* I player diventano "intent/comandi" che alimentano la simulazione; nessuna logica di gioco nel layer di rendering.
* Nessun cambiamento di gameplay: solo pulizia architetturale per rendere banale il passo successivo.

### M12 — Online co-op *(fase 2, ambiziosa)*

* Server authoritative: la simulazione (inclusa la corda) gira **solo** sull'autorità; i client inviano input e renderizzano lo stato ricevuto.
* High-level Multiplayer API + `MultiplayerSynchronizer`; gestione lag/interpolazione lato client.
* Config di rete via env/config (no segreti nel repo).

---

## 5. Note tecniche sulla corda

È il punto di rischio. Linee guida:

* Preferire un **vincolo logico di distanza** semplice e stabile rispetto a una simulazione di corda fisicamente accurata: per il gameplay conta che sia  *leggibile e prevedibile* , non realistica.
* Tenere fisso lo step fisico (`physics/common/physics_ticks_per_second`) per determinismo.
* Evitare di simulare la corda due volte (una per lato): un solo vincolo nel mondo authoritative.
* Per la versione visiva (catena/verlet), disaccoppiarla dalla fisica: è decorazione che segue i due punti d'ancoraggio.

---

## 6. Regole operative per Claude Code

* Lavora  **una milestone alla volta** , poi fermati e riepiloga cosa è stato fatto e come testarlo.
* Prima di operazioni distruttive o di refactor ampi,  **chiedi conferma** .
* Commit atomici con Conventional Commits in italiano a fine di ogni step significativo.
* Se una scelta tecnica ha alternative reali (es. approccio corda), proponile brevemente e chiedi prima di implementarne una.
* Mantieni il README aggiornato a ogni milestone (come avviare, controlli, stato).
