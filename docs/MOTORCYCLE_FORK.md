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
| Mouse cursor moving across a 3×3 grid (`Attempt.CursorPosition`) | Handlebar/steering input moving the bike across track lanes |
| `Grid` (`scripts/game/ui/Grid.cs`) drawing the cursor | `MotorcycleController` (`scripts/game/ui/MotorcycleController.cs`) driving bike lane position + lean |
| `CameraLock` (`scripts/game/camera/CameraLock.cs`) | `CameraChase` — a chase camera that follows behind/above the bike |
| `Note` objects + `NoteRenderer` | Track gates/obstacles + `GateRenderer`, hit in time with the beat |
| `HitJudgment` / `ScoreJudgment` / `HealthJudgment` | Unchanged — a "hit" becomes clearing a gate/obstacle on time instead of clicking a note |

## Status

This is an early skeleton, matching the rest of the codebase's current stage
(most rendering logic upstream is still pseudocode). The pieces above exist
as starting points to build the motorcycle-specific gameplay on, not a
finished mode.
