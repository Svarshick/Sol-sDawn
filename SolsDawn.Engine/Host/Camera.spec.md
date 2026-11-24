# Camera Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `CameraController.cs`, `CartesianCamera.cs`. `GameConfig.Camera` is designer content in `SolsDawn.Gameplay`.

Requirements are `CAM-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Axis | X or Y. Each axis runs on its own, with the model in its slot. |
| Extent, `E` | The visible width of the camera in world units for X, the visible height for Y, at the current zoom. |
| `c`, `p` | The camera and player positions along the axis. |
| `Δp` | The player's displacement along the axis since the previous camera update. |
| `x` | `p − c`: the player in camera coordinates. |
| `o` | `c − p`: the camera in player coordinates. |
| `tr`, `fd` | `Trigger / 100 · E` and `Distance / 100 · E`. |
| Side, `s` | The sign of `x` when tracking starts. |
| Envelope | A movement model of the `Envelope` category, `Ads` (MOV-01). |
| Velocity, `v` | The camera's velocity along the axis in player coordinates, from the envelope, in world units per second. |
| Carry | Whether the camera adds `Δp` to its position. |

## Models

- **CAM-01** `CameraSpecs` is a model type with the `Camera` category. It has `Zoom`, from 0.1 to 2, and two slots of category `Camera axis`: `X` and `Y`.
- **CAM-02** `Follow` and `Forward` are the camera axis model types. `Follow` is `[Default]`. Both have `Trigger` and `Distance`, from 0 to 50, in percent of `E`.
- **CAM-03** `Trigger >= Distance` always. Through CFG-66, an edit that sets `Trigger` below `Distance` lowers `Distance` to it, and an edit of `Distance` above `Trigger`, or a load with `Trigger < Distance`, raises `Trigger` to it.
- **CAM-04** `Follow` has one envelope slot, `Movement`. `Forward` has two, `Reach` and `Lead`.
- **CAM-05** Camera models hold only configuration. The state of each axis is kept by the camera, is never saved and is reset by CAM-11.
- **CAM-06** The camera reads `Zoom` and the axis slots' active models on every update, so an edit or a swap takes effect at once (CFG-67). A stage the active model does not have counts as holding (CAM-25).

## Startup and frame

- **CAM-10** `RunLevels(start, camera)` takes the `CameraSpecs` slot. The camera updates in the after phase, after `CombatSystem.Resolve` (LVL-04).
- **CAM-11** When `G.Player` is alive and is not the player of the previous update, as after a spawn or a level restart, the camera moves to the player, and both axes become idle with `v = 0` and zero-valued memory. While there is no player, the camera does not move.

## Axis

- **CAM-20** An axis is idle or tracking. An idle axis does not move the camera.
- **CAM-21** Stages, with the target `o*` of each and the envelope that moves the camera toward it:

  | Model | Stage | `o*` | Envelope |
  |---|---|---|---|
  | `Follow` | 1 | `−s · fd` | `Movement` |
  | `Forward` | 1 | `0` | `Reach` |
  | `Forward` | 2 | `s · fd` | `Lead` |

  `Follow` keeps the player `fd` ahead of the camera, `Forward` puts the camera `fd` ahead of the player.
- **CAM-22** Each update runs these steps in order:
  1. Carry (CAM-24).
  2. Trigger (CAM-23).
  3. Approach the stage target (CAM-25).
  4. End holding (CAM-26).
- **CAM-23** Trigger: when `|x| >= tr` and `sign(Δp) = sign(x) ≠ 0`, the axis starts tracking with `s = sign(x)` at stage 1 with carry on. This interrupts the current tracking, but `v` and the envelope memory are kept, so a reversal runs the envelope's turn. It does not fire while the axis is tracking the same side with carry on.
- **CAM-24** Carry: while tracking, the first update with `sign(Δp) ≠ s` turns carry off until the next trigger. `Δp = 0` counts. With carry on, `c` moves by `Δp`. Carry runs before the trigger, so the update that starts tracking does not move by the `Δp` that set it off.
- **CAM-25** Approach: in a stage, with `o` after carry:
  1. On the first update of the stage, the direction `D = sign(o* − o)` is fixed.
  2. When `D = 0` or `sign(o* − o) ≠ D`, the target is reached without an envelope step. The player's own movement may pass `o*`, which is accepted.
  3. Otherwise `v = Calculate(v, D, dt, ref memory)` of the stage's envelope, and `o` moves by `v · dt`. A step that would reach or pass `o*` stops at `o*`, and the target is reached.

  Reaching the target of a stage that is not the last one starts the next stage with `v` and the memory kept. Reaching the target of the last stage sets `v = 0` and the memory to zero, a hard stop, and the axis holds.
- **CAM-26** A holding axis moves only by carry, so it keeps `o` while the player keeps moving toward `s`. When carry is off, the axis becomes idle.
- **CAM-27** So when the player stops or reverses during a stage, the camera finishes the approach in world coordinates and then becomes idle. It becomes idle at once when it was holding.

## Verification

- CAM-03, CAM-21 to CAM-27: automated tests with xUnit v3 that run one axis directly with a fixed `dt`, using phase models defined in the test project.
- CAM-01, CAM-02, CAM-04 to CAM-06, CAM-10, CAM-11: manual check in the running game.

## Known limits

- CAM-24 compares the sign of `Δp` exactly, with no tolerance. If physics makes the player jitter slightly along an axis, especially Y, a single frame with `Δp = 0` or the opposite sign turns carry off and ends tracking early.
