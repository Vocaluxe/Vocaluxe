# Vocaluxe — lokaler Linux-Build (.NET 10)

Fork von [Vocaluxe/Vocaluxe](https://github.com/Vocaluxe/Vocaluxe), Remote ist
`byte55/Vocaluxe`. Gearbeitet wird auf **`feature/768-net10-crossplatform`**,
dem .NET-10-Cross-Platform-Port.

## Priorität

**Es muss auf diesem Rechner laufen.** Das ist der Maßstab, nicht die
Upstream-Kompatibilität. Upstream-Merges dauern zu lange, deshalb werden Fixes
hier lokal gemacht und direkt auf den Fork gepusht. Pushes sind ausdrücklich in
Ordnung.

Der Rechner ist ein dedizierter Karaoke-Rechner — Hardware und Systemumgebung
stehen in `~/CLAUDE.md`. Kurzfassung: Haswell-i5, **nur Intel-iGPU**, Ubuntu
26.04, **GNOME auf Wayland**, PipeWire.

## Bauen

Kompletter Build inklusive nativer Helfer und fertiger Distribution:

```bash
./.build/build-linux.sh    # -> dist/Vocaluxe/, Launcher dist/Vocaluxe.sh
./dist/Vocaluxe.sh
```

Für schnelle Iteration bei reinen C#-Änderungen reicht der managed Build:

```bash
dotnet build Vocaluxe/Vocaluxe.csproj -c Release
```

Der landet aber in `Vocaluxe/bin/Release/net10.0/` **ohne** Spieldaten und ohne
die `.so`-Dateien. Zum Starten von dort einmalig danebenlegen:

```bash
cp -ru Output/. Vocaluxe/bin/Release/net10.0/
```

Auf dem Desktop und im Anwendungsmenü liegt ein Starter (`vocaluxe.desktop`)
auf `dist/Vocaluxe.sh`. Nach einem Rebuild zeigt der automatisch auf die neue
Version — solange der Pfad `dist/Vocaluxe.sh` bleibt, ist nichts anzupassen.

Die nativen Helfer nur neu bauen, wenn du an C-Code angefasst hast:

```bash
make -C PitchTracker                      # -> libPitchTracker.dll.so
make -C Vocaluxe/Lib/Video/Acinerella     # -> libacinerella.so
```

Beide kopieren sich selbst nach `Output/`. `dist/` und die `.so` in `Output/`
sind gitignored.

### Abhängigkeiten

Bereits installiert; hier nur zur Vollständigkeit, falls neu aufgesetzt wird:

```bash
sudo apt install -y dotnet-sdk-10.0 build-essential \
    libavcodec-dev libavformat-dev libswscale-dev libavutil-dev \
    libswresample-dev libportaudio2 libfontconfig1
```

`dotnet-sdk-10.0` kommt aus dem Ubuntu-Archiv, kein Microsoft-Repo nötig.

## Architektur, soweit für Änderungen relevant

Managed ist fast alles: **OpenTK 4** (Fenster + OpenGL, bringt GLFW als Native
mit), **SkiaSharp** (Rendering), **PortAudioSharp2** (Audio-Ausgabe),
Microsoft.Data.Sqlite, Roslyn für die zur Laufzeit kompilierten Party-Modes.

Nativ und selbst zu bauen sind nur zwei Dinge:

| Bibliothek | Zweck | Quelle |
|---|---|---|
| `libPitchTracker.dll.so` | Pitch-Erkennung, Grundlage des Scorings | `PitchTracker/` |
| `libacinerella.so` | Audio- **und** Video-Decode über ffmpeg | `Vocaluxe/Lib/Video/Acinerella/` |

Es gibt **kein zweites Decode-Backend**. Fällt Acinerella aus, gibt es weder
Ton noch Video.

Der ffmpeg-6-Port von Acinerella ist an echtem Material erprobt: Songs mit
Video und Tonausgabe laufen. Damit ist auch die Layout-Fallback-Logik in
`ac_create_audio_decoder` praktisch bestätigt, nicht nur kompilierbar.

Kein SDL2 — das taucht nur noch in Kommentaren auf.

## Lokale Fixes und wo es weh tut

Drei Fixes liegen als Commits auf dem Branch; sie sind nicht
maschinenspezifisch, sondern treffen jeden Linux-Build mit aktuellem ffmpeg:

- **Acinerella auf ffmpeg 6+ portiert.** Die `int64_t`-Channel-Layout-Bitmaske
  ist in ffmpeg 5.1/6.0 der `AVChannelLayout`-Struct gewichen. Ubuntu 26.04
  liefert **ffmpeg 8**, der alte Code kompilierte gar nicht mehr. Wenn du an
  `acinerella.c` arbeitest: die Datei stammt aus der ffmpeg-2/4-Ära, weitere
  API-Brüche sind wahrscheinlich. `av_init_packet` ist der nächste Kandidat,
  es warnt bereits als deprecated.
- **PitchTracker mit `g++` statt `gcc` gelinkt.** Alle Objekte sind C++, `gcc`
  zieht libstdc++ nicht mit. Die `.so` hatte ~40 ungelöste Symbole und wäre
  erst beim `dlopen` zur Laufzeit gescheitert, nicht beim Build.
- **GLFW-Error-Callback in `COpenGL.cs`.** OpenTK macht per Default aus *jedem*
  GLFW-Fehler eine Exception. Unter Wayland fragt die Fenstererzeugung die
  Fensterposition ab, die das Protokoll Clients bewusst nicht gibt — der Start
  starb in „Init Draw". Jetzt wird geloggt statt geworfen. **Vocaluxe läuft
  damit nativ unter Wayland, XWayland ist nicht nötig.**

Bei weiteren Wayland-Themen (Fullscreen, Maus-Grab) zuerst ins Log schauen, ob
wieder eine `FeatureUnavailable`-Meldung dahintersteckt.

## Laufzeit-Konfiguration

Liegt **nicht** im Repo, sondern unter `~/.config/Vocaluxe/`:

- `Config.xml` — u. a. die `SongFolder`-Einträge. Die echte Bibliothek ist
  `~/UltraStar Songs` (Leerzeichen im Pfad, immer quoten). Die beiden
  Standard-Einträge daneben sind leer und harmlos.
- `Logs/Vocaluxe.log` — die einzige brauchbare Fehlerquelle. Das Programm
  schreibt **nichts** nach stdout/stderr und beendet sich bei einem Absturz
  mit Exit-Code 0. Ein stiller, schneller Exit heißt also nicht „ok".
- `Logs/Song.log` — Parser-Warnungen zu einzelnen Songdateien.

`Renderer` in der `Config.xml` kennt unter Linux nur `TR_CONFIG_OPENGL` und
`TR_CONFIG_SOFTWARE`. Direct3D steht im Enum hinter `#if WIN` und existiert
hier nicht.

### Abgebrochener Prozess blockiert den nächsten Start

Die Single-Instance-Sperre ist ein benannter Mutex, den .NET unter Linux als
Datei in `/tmp/.dotnet/shm/session*/Vocaluxe-SingleInstanceMutex` ablegt. Wird
der Prozess **hart beendet** (SIGTERM/SIGKILL, etwa durch `timeout` in einem
Testskript), bleibt der Mutex als *abandoned* zurück. Der nächste Start stirbt
dann daran, **bevor das Logging initialisiert ist**: Exit-Code 0 nach ~0,1 s,
kein Log-Eintrag, und nicht einmal die vorgesehene Meldung „Another Instance of
Vocaluxe is already runnning!", weil der reguläre Zweig gar nicht erreicht wird.

Beim normalen Schließen des Fensters passiert das nicht. Falls es doch klemmt:

```bash
rm -rf /tmp/.dotnet/shm/session*
```

Wer Vocaluxe automatisiert testet, sollte das einkalkulieren — zwei
aufeinanderfolgende `timeout`-Läufe sehen sonst wie ein sporadischer Absturz
aus.

## Audio-Eingang (Mikrofone)

Interface: **Behringer Xenyx QX1002USB**, USB-Codec ist ein TI PCM2902
(`08bb:2902`). Meldet sich als ALSA-Card `CODEC` und in PipeWire als
„PCM2902 Audio Codec Analog Stereo". Kann **16 Bit, 48 kHz, Stereo** — mehr
nicht, das reicht aber für zwei Spieler.

Mikrofon: **the t.bone MB 45 II**, dynamisch, Superniere. Braucht **keine**
Phantomspeisung, +48 V bleibt aus.

### Falle: `USB/2-TR TO MAIN MIX` schaltet die Aufnahme stumm

Der Mixer hat zwei Taster in der Gruppe `USB/2-TR`. Der zweite,
**`TO MAIN MIX`, muss ausgerastet sein.** Aus dem Handbuch:

> USB/2-TR TO MAIN MIX button routes USB/2-Track playback to MAIN MIX and
> **mutes the 2-TR OUT/USB recording signal.**

Gedrückt verhält sich der Aufbau wie ein Defekt: Mixer arbeitet, alle Lampen
reagieren, der Kompressor zeigt Signal — und der Rechner bekommt trotzdem
digitale Stille bei −90 dBFS. Zum Mithören des Rechnertons ist der *erste*
Taster (`TO PHONES/CTRL RM`) zuständig, der den Aufnahmeweg nicht antastet.
Für Karaoke wird keiner von beiden gebraucht, der Ton kommt direkt aus dem
Rechner.

### Spielertrennung über Panorama

Der USB-Aufnahmeweg trägt die **Hauptmischung**, sein Pegel hängt am
MAIN-MIX-Fader. Beide Mikrofone landen deshalb per Default summiert auf beiden
Kanälen — für Vocaluxe unbrauchbar, beide Spieler sähen dasselbe Signal.

Trennung entsteht erst durch hartes Panning: **MIC 1 ganz nach links,
MIC 2 ganz nach rechts.** Gemessen mit MIC 1 hart links: 59 % Spitzenpegel
links gegen 0,9 % rechts, also **36 dB Kanaltrennung** — für die
Tonhöhenerkennung mehr als genug.

Pegel prüfen ohne Vocaluxe:

```bash
arecord -D pipewire -f S16_LE -c 2 -r 48000 -d 6 /tmp/mic.wav
```

`-D pipewire` statt `-D hw:CODEC,0`, dann kollidiert es nicht mit einer
laufenden Instanz — der PCM2902 lässt sich nur exklusiv öffnen.

## Offen

- **Zweites Mikrofon** noch nicht angeschlossen: MIC 2, PAN hart rechts, dann
  Zuordnung der beiden Kanäle in den Aufnahme-Optionen von Vocaluxe.
- Theme-Videos (`BG_Video.mp4`, `IntroIn/Mid/Out.mp4`) fehlen im Repo, das Log
  meldet „Expect visual problems". Rein kosmetisch.
