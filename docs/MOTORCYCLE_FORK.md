# Motorhytha: Motorcycle Rhythm Fork

This repository is a fork of [Rhythia](https://github.com/Rhythia/Client) (the
mouse/3×3-grid rhythm game built on Godot 4.6 + C#). We are **not** changing
Rhythia's underlying framework — the `Attempt` state object, the
`GameComponent` game loop, the `UIComponent`/`Renderer` pipeline, and the
judgment/mod/scoring systems are all reused as-is. What we're building on top
is a new game mode: a **motorcycle rhythm racer**, where the player steers a
bike down a track in time with the music instead of moving a mouse cursor
around a grid.

## Why fork instead of build from scratch

Rhythia already solves the hard, boring parts of a rhythm game: audio timing,
map/replay parsing, judgments (hit/health/score), mods, camera handling,
settings, and the Godot project scaffolding. Reusing that means the
motorcycle mode can focus entirely on new gameplay: steering, lanes, and
obstacle/gate charts, instead of re-deriving timing and scoring.

## Mapping old mechanic -> new mechanic

| Rhythia concept | Motorcycle concept |
|---|---|
| Mouse cursor moving continuously across a 3×3 grid (`Attempt.CursorPosition`) | A/D keys moving the bike between 3 discrete lanes (`Attempt.BikeLane`) |
| 3×3 grid (3 columns × 3 rows) | Collapsed to 3×1: a chart's note **X** (column, -1/0/1) becomes the lane, **Y** (row) is ignored, via `MotorcycleLanes.LaneFromNoteX` |
| `Grid` (`scripts/game/ui/Grid.cs`) drawing the cursor | `MotorcycleController` (`scripts/game/ui/MotorcycleController.cs`) driving bike lane position, lean, and chase camera |
| `Note` objects + `NoteRenderer` | Same `Note` objects (unchanged map format), reused as track gates and drawn by `GateRenderer` |
| `HitJudgment` / `ScoreJudgment` / `HealthJudgment` (unimplemented stubs upstream) | `MotorcycleHitJudgment` — basic lane-vs-timing check that resolves each gate as the bike reaches it |
| `GameComponent.Play()` | Now also loads `Attempt.Map.Notes` (decoded by the existing `MapParser`) into `Attempt.Objects[typeof(Note)]`, so old map files work unchanged |

## Status

Basic playable loop: an old map's notes load in, the clock (`Attempt.Progress`)
advances every frame, the bike moves between 3 lanes with A/D, and gates
resolve as hit/missed when they reach the bike. Scoring, health, audio
playback sync, and actual track/bike art are still not wired up.
