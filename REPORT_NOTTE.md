# Report notte autonoma — 10/06/2026 (01:04 → 02:34, chiusura verifiche 08:54)

Sessione autonoma sul progetto Controluce secondo la lista: **1) Grafica, 2) Skin e personaggi, 3) Ottimizzazione, 4) Extra a giudizio**. Dieci cicli completati, 20 commit su `main`, suite di test sempre verde prima di ogni commit (12 test headless + 2 di rete). Nessuna operazione distruttiva, nessun push, `Documentation.md` non toccato.

Nota sulla tempistica: il lavoro implementativo si è concluso alle 02:34 (Ciclo 10). Le tre verifiche visive rimaste in sospeso (bloccate da un problema tecnico di lettura immagini, vedi "Problemi") sono state completate alle 08:54: **tutte e tre passano, nessun fix necessario**.

---

## Lavoro per ciclo

### Ciclo 1 — Grafica: ambiente "controluce" (01:10–01:40)
- **Commit:** `7d4f641`, `d3938d7`, `9c5b5c4`, `14ffa44` (docs)
- Environment nuovo in `scenes/main.tscn`: cielo caldo all'orizzonte / blu notte in alto, **sole basso davanti ai player** (avanzano verso −Z, quindi giocano davvero "controluce"), tonemap ACES, glow, SSAO, nebbia leggera per profondità, ombre morbide (`shadow_blur 2.5`), luce fill fredda senza ombre.
- Materiali blocchi di fase: **condivisi e cached per fase** (anche perf), emissione tenue + rim sui blocchi colorati; geometria fantasma riscritta con **shader fresnel** (bordi luminosi, centro trasparente) — molto più leggibile della vecchia trasparenza piatta.
- Player con rim+emissione; corda emissiva quando tesa o in riavvolgimento (feedback).
- Verifica: screenshot prima/dopo in `/tmp/controluce_shots/` (`baseline_*` vs `c1_env_*`, `c1_mat_room2_*`, `c1_final_*`). La baseline era piatta (cielo grigio, fantasmi quasi invisibili); il salto è netto.

### Ciclo 2 — Skin e personaggi (01:15–01:34)
- **Commit:** `d3bea01`, `f8b363b`, `5ab1c08` (docs)
- `PlayerSkin.cs`: **5 skin procedurali** (Classica, Antenna, Tuba, Corna, Aureola), nessun asset binario, materiali corpo cached per fase.
- **Scambio ruoli** blu/rosso: `GameManager.ApplyAppearance` inverte fase, collisioni, colori e camere. Gli spawn NON si scambiano (vedi bug fantasma sotto).
- Menu **"Personaggi"**: skin separate per P1/P2 + checkbox scambio ruoli, persistenza in `user://settings.cfg`.
- **Sync online**: l'host detta la propria skin P1 e lo scambio ruoli, l'ospite porta la propria skin P2 (`AnnounceSkin` → `SyncAppearance`).
- Test nuovo `test_skins.tscn` (fasi/maschere invertite, lati di spawn preservati, accessori presenti, camera coerente). Screenshot: `c2_swap_p1/p2.png`, `c2_menu_personaggi.png`.

### Ciclo 3 — Ottimizzazione misurata (01:35–01:40)
- **Commit:** `d23421f`
- Harness `test_perf.tscn` (draw call, primitive, tempi caricamento, contatore rebuild).
- **Misure prima → dopo:**
  - primitive/frame: **419.784 → 104.904 (−75%)**
  - rebuild di PhaseBlock in 5 s su room_02 (blocchi a toggle): **6 → 0** (ora `Recolor` in place: solo layer+materiali, niente ricreazione di nodi/shape)
  - LoadRoom: 11–13 ms (invariato, già ok); draw call: 218 (invariato, stesso numero di oggetti)
- Online: il SubViewport nascosto **renderizzava l'intera scena ogni frame** → ora `RenderTargetUpdateMode.Disabled`.
- Mesh procedurali (occhi, polvere, accessori skin) a segmenti ridotti.

### Ciclo 4 — run_tests.sh + indicatore partner (01:41–01:51)
- **Commit:** `2e84812`, `52ca8cc` (docs)
- `./run_tests.sh`: intera suite + test di rete su loopback, log in `/tmp/`, exit code aggregato.
- **PartnerIndicator** (per ciascuna vista): freccia agganciata al bordo se il compagno è fuori inquadratura o dietro; **anello "a raggi X"** sul punto proiettato se è inquadrato ma occluso (raycast con la maschera di ciò che il viewer vede solido — rispetta l'asimmetria delle fasi). Funziona in split-screen e online. Screenshot: `c4_indicatore_partner.png`.

### Ciclo 5 — room_04 "Il pendolo" (01:52–01:56)
- **Commit:** `1f0ffe0`, `e8b7838` (docs)
- Stanza costruita sul dondolio e sul salto da appeso: P1 fa da ancora su un ponte alto neutro, P2 attraversa il vuoto a pendolo verso un approdo rialzato; poi colonne a fase alternante sfalsate. Aggiunta a `RoomPaths`. Screenshot: `c5_room04_overview.png`.

### Ciclo 6 — Test gameplay room_04 + export (01:57–02:05)
- **Commit:** `24e6f34`, `5ddab05`
- `Room04Test`: la stanza è **dimostrata risolvibile con le meccaniche reali** — lo script gioca come una coppia vera (P1 contrasta il tiro per restare ancorato, P2 riavvolge la corda per accorciare il pendolo, pompa in risonanza, salta, si fa calare sull'approdo). Servite 3 iterazioni di strategia (dettagli in "Problemi").
- `export_presets.cfg` versionato (macOS universal, Windows x86_64, Linux x86_64; tolto dal `.gitignore` di proposito: nessuna credenziale dentro). README con istruzioni di export.

### Ciclo 7 — Portale d'uscita + restyle menu (02:06–02:10)
- **Commit:** `a13acbb`
- `ExitZone`: **colonna di luce calda pulsante** (prima l'uscita era invisibile!), si intensifica al completamento.
- Menu: sfondo gradiente notte→orizzonte caldo, titolo con ombra, bottoni con hover/focus ambra. Screenshot: `c7_menu_restyle.png`.

### Ciclo 8 — Hint 3D + fullscreen + tema condiviso (02:11–02:16)
- **Commit:** `5ec954e`, `1dd0740` (docs)
- Cartelli `Label3D` billboard: 3 in room_01 (fasi, corda-ancora, riavvolgimento Q/Ctrl), 2 in room_04 (strategia pendolo).
- Opzione **"Schermo intero"** nel menu Opzioni, applicata live, persistita in `[video] fullscreen`.
- Tema bottoni estratto in `assets/ui_theme.tres`, condiviso tra menu principale e menu pausa.
- Verifica visiva completata alle 08:54: hint leggibili e ben piazzati (`c8_hints_p1.png`), pannello Opzioni corretto (`c8_options_fullscreen.png`).

### Ciclo 9 — Audio procedurale + lobby (02:17–02:24)
- **Commit:** `2a6d555`, `9307f19`
- `AudioSynth.Fanfare` (arpeggio C-E-G-C al completamento stanza) e `Hum` (ronzio in loop senza click del portale, cicli interi su 2 s). Nessun asset binario.
- **Banner di stato lobby online**: host "In attesa dell'ospite sulla porta N..." (ri-mostrato anche su disconnessione), client "Connessione a host:porta...". Prima la lobby era muta.

### Ciclo 10 — room_05 "La porta sul vuoto" (02:25–02:34)
- **Commit:** `6c8a584`
- Quinta stanza, **ruoli invertiti rispetto alla 4**: il rosso sale sul ponte e presidia una pedana a peso (ancora + tiene giù una porta), il blu attraversa appeso con riavvolgimento e dondolio. Zattera mobile finale da prendere in coppia. Hint 3D inclusi.
- `Room05Test` scriptato PASS; fix a `Room04Test` (assert sul banner: la 4 non è più l'ultima stanza).
- Verifica visiva completata alle 08:54: layout conforme (`c10_room05_overview.png`).

---

## Problemi trovati (e come sono stati risolti)

1. **Contatti fantasma da teletrasporto incrociato** (il bug più interessante della notte). Con lo scambio ruoli, scambiare anche i lati di spawn teletrasportava ciascun player nella colonna dell'altro **nello stesso frame fisico**: Godot creava una coppia di contatto fantasma reciproca e i due player "si cavalcavano" salendo all'infinito (+2 m/tick con velocità zero e `IsOnFloor()=true`). Diagnosticato strumentando il test con posizioni e collider ("P1 tocca: Player2"). **Decisione:** lo scambio ruoli inverte solo fase/colori/camere; gli spawn restano per lato (sono su piattaforma neutra condivisa, quindi nessuna differenza di gameplay). Documentato nel codice.
2. **Fantasmi sbiancati dal glow**: il primo shader fresnel spingeva l'albedo fino a 1.6 e il bloom desaturava tutto in bianco. Ridotti i moltiplicatori (`0.55 + 0.45·fres`).
3. **Strisce orizzontali nel menu**: `stretch_mode = 1` su TextureRect in Godot 4 è TILE, non scale. Rimosso.
4. **Recolor in place perdeva il reset del lampeggio** dei blocchi a toggle: aggiunto reset esplicito di `Transparency` al cambio fase.
5. **Script di Room04Test, 3 iterazioni**: (a) P1 senza input veniva trascinato via dalla tensione della corda → comando di tenuta; (b) P2 si incollava alla parete frontale dell'approdo, il salto non scattava mai → riavvolgere la corda per alzare il pendolo sopra il bordo; (c) la corda corta gli impediva poi di atterrare → fase di rilascio totale sopra la piattaforma. Insight: **il tira-corda è LO strumento chiave** di queste stanze, e per giocatori veri sarà più facile che per uno script.
6. **Room05Test**: P2 senza comando di tenuta veniva trascinato giù dalla pedana → tenuta su entrambi gli assi dal tick 30.
7. **Lettura immagini bloccata** a metà sessione (screenshot 2880×1800 finiti nel contesto hanno fatto rifiutare ogni lettura successiva): le verifiche visive del Ciclo 8 e 10 sono state salvate su disco e completate a fine sessione (tutte ok, nessun fix necessario).

## Decisioni di design da conoscere

- **Porta di room_05 scavalcabile in speedrun**: con riavvolgimento + salto perfetto si può superare la porta chiusa (top a y=3, ancora della corda a 5.5). Accettato deliberatamente come "tech": alzare la porta sbarrerebbe il ponte stesso (il ponte passa a y=4 sopra la porta). Il percorso normale resta bloccato.
- **export_presets.cfg versionato** togliendolo dal `.gitignore` (il template lo ignora perché può contenere credenziali di firma; il nostro non ne ha — commentato nel `.gitignore`).

## Rimandato / suggerimenti

- **Variazione palette per stanza** (alba→notte progressiva lungo le 5 stanze = narrazione visiva): era il candidato del Ciclo 11, rimandato perché richiedeva le verifiche screenshot mentre la lettura immagini era bloccata. È il prossimo lavoro grafico a maggior valore.
- **Tuning difficoltà**: non fatto di proposito — serve un playtest umano (i test scriptati dimostrano la risolvibilità, non la difficoltà percepita). Suggerimento: provare room_04 in coppia e valutare se il periodo delle colonne a toggle (2.4 s) è troppo stretto.
- **Polish TensionBar** (stile coerente col nuovo tema UI): piccolo, mai iniziato.
- **Screenshot aggiornato del pannello Personaggi** nel set di verifica (quello salvato è precedente al restyle del menu).

## Stato finale

- Branch `main`, working tree pulito, **nessun push** (come da regole).
- Suite: `./run_tests.sh` → 12 test headless + server/client di rete, **tutti verdi** all'ultima esecuzione.
- Screenshot significativi in `/tmp/controluce_shots/` (citati sopra; attenzione: è una cartella temporanea, copiarli se servono).
