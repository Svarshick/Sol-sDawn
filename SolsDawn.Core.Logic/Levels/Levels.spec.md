# Levels Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `SolsDawn.Core.Logic/Levels/`, the destroy handling of `Entity` in `Entity.cs` and `Player.cs`.

Requirements are `LVL-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Level | A public static method with no parameters that returns `Job` and is marked `[Level]`. Levels are designer content in `SolsDawn.Gameplay`. |
| Level job | The job a level method returns while it runs. |
| Level runner | `LevelRunner.Run`, the root job of the game. |
| Phase | One of the runner's subjobs: Effects, player, level host or after. |
| Spawn | The `Spawn` functions a level calls to create platforms, the player and enemies. |

## Startup

- **LVL-01** `SolsDawn.Gameplay` owns startup. `Main.RootJob` initializes the Library with the `_Data` path (CFG-90), loads the root `GameConfig` (CFG-18), initializes the graph store with the `_Data/Graphs` path (SPG-20) and returns `LevelRunner.Run(start)`, where `start` is the first level to run.
- **LVL-02** Levels are found by reflection in the loaded assemblies when the runner starts. A method marked `[Level]` with the wrong signature is logged as an error and not listed.

## Frame order

- **LVL-03** The engine updates input before the root job, every frame.
- **LVL-04** The runner creates its phases in this order, and every frame they run in this order:
  1. Effects: the owner job of `Effects` (CMB-41, CMB-45).
  2. Player: when `G.Player` is alive, its `PlayerController` updates (CMB-22). Then the speed recording takes its sample (SPG-01).
  3. Level host: the current level job and every subjob it started, such as enemy behaviours.
  4. After: `CombatSystem.Resolve` (CMB-10), then the camera moves to `G.Player` when it is alive.

  This works because a job updates its subjobs in the order they were created, depth-first, before its own code.

## Levels

- **LVL-05** One level runs at a time, inside the level host, so every level keeps position 3 of LVL-04.
- **LVL-06** Everything a level job creates, directly or through Spawn, is a resource of that job. When the level job ends or is killed, it is destroyed. A level that should keep running must not return.
- **LVL-07** The debug window lists every level and has a Restart button. Choosing a level or Restart takes effect at the next update of the level host: the current level job is killed and the chosen level starts.

## Spawn

- **LVL-08** Spawn functions create objects in the current job, so they must be called from a level job or one of its subjobs.
- **LVL-09** `Spawn.Platform(position, width, rotation = 0)` creates a platform.
- **LVL-10** `Spawn.Player(position, specs)` creates the player with a `PlayerSpecs` slot, creates its `PlayerController` and sets `G.Player`. Calling it while `G.Player` is alive is a fatal error that names the current level. When the player is destroyed, `G.Player` becomes null.
- **LVL-11** `Spawn.Enemy(position, specs, behaviour)` creates an enemy with an `EnemySpecs` slot and starts `behaviour(enemy)` as a subjob of the current job. It may be called any number of times. When the enemy is destroyed, its behaviour job is killed.

## Entities

- **LVL-12** Destroying an entity kills its state job, so whatever the state job created is destroyed too.

## Verification

- LVL-01 to LVL-12: manual check in the running game.

## Known limits

- `Effects` and visuals in the animations pool are not owned by the level job, so they outlive a level switch. Both are in the backlog.
