# Movement Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `Movement.cs`, `MotionSolver.cs`, `Adsr.cs`, `PlayerStates.cs`.

Requirements are `MOV-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Movement model | A model that computes velocity along one axis, once per frame, for a `PlayerState`: `MotionSolver` or `Adsr`. |
| Phase model | A model in a slot of a movement model. It computes velocity for one `MotionSolver` phase or one ADSR state. |
| Velocity | Signed speed along the movement axis: the platform tangent in `RunState`, X in the air. |
| Speed, `s` | The absolute value of velocity. |
| `inDir` | Input direction: -1, 0 or 1. |
| Held, released | `inDir != 0`, `inDir == 0`. |
| `C` | The `ControlledSpeed` of the movement model. |
| Memory | A `MovementMemory` value: the state of a movement model between frames of one Job. |
| A / D / S / R | Attack, Decay, Sustain, Release. |

## Movement models

- **MOV-01** `MotionSolver` and `Adsr` are model types with the `Movement` category. Both implement `IMovementModel`.
- **MOV-02** Each `PlayerState` that moves has one slot of category `Movement`. The slot accepts any movement model, and its default model is `MotionSolver`.

  | Model type | Slot |
  |---|---|
  | `RunState` | `Movement` |
  | `CelesteJumpState` | `HorizontalMovement` |
  | `PrimitiveJumpState` | `HorizontalMovement` |
  | `FallState` | `HorizontalMovement` |
  | `BounceState` | `HorizontalMovement` |

- **MOV-03** `IMovementModel` has one operation: `Calculate(velocity, inDir, dt, ref memory)` advances one frame, updates `memory` and returns the new velocity.
- **MOV-04** A `PlayerState` Job creates one zero-valued memory when it starts, keeps it as a local value and passes it to every `Calculate`. It reads the slot's active model on every frame, so a model swapped while the game runs takes effect at once (CFG-67).
- **MOV-05** Movement models and phase models hold only configuration. All movement runtime state is in the memory, which is never saved and never shared between Jobs, so any number of slots and entities may reference the same model.
- **MOV-06** A movement model accepts any memory value on any frame, including one written by another movement model type before a swap.

## MotionSolver

- **MOV-10** `MotionSolver` has `ControlledSpeed` and six slots of `IMotionModel`, one for each combination of speed range (Normal, GreatSpeed) and phase (Drive, Brake, Turn). The current phase is kept in memory, and zero-valued memory means Drive.
- **MOV-11** The speed range is GreatSpeed when `s > C`, otherwise Normal.
- **MOV-12** The phase is chosen each frame. The first matching row wins.

  | # | Condition | Phase |
  |---|---|---|
  | 1 | Released | Brake |
  | 2 | `inDir == sign(velocity)` and `s >= C` | Drive |
  | 3 | `inDir == -sign(velocity)`, or the current phase is Turn | Turn |
  | 4 | Otherwise | Drive |

- **MOV-13** The slot for the speed range and phase computes the new velocity.
- **MOV-14** A slot accepts a phase model only if the model allows the slot's phase and speed range (CFG-10).

  | Slot | Default model |
  |---|---|
  | `GreatSpeedDrive` | `FrictionGreatSpeedDrive` |
  | `GreatSpeedBrake` | `FrictionGreatSpeedDecay` |
  | `GreatSpeedTurn` | `FrictionGreatSpeedDecay` |
  | `NormalDrive` | `LinearDriveModel` |
  | `NormalBrake` | `LinearBrakeModel` |
  | `NormalTurn` | `LinearTurnModel` |

- **MOV-15** `MotionSolver` phase models:

  | Model type | Allowed | Result |
  |---|---|---|
  | `FrictionGreatSpeedDrive` | GreatSpeed: Drive | `inDir * max(C, s - Friction * dt)` |
  | `KeepGreatSpeedDrive` | GreatSpeed: Drive | `velocity` |
  | `FrictionGreatSpeedDecay` | GreatSpeed: Brake, Turn | `sign(velocity) * max(C, s - Deceleration * dt)` |
  | `LinearDriveModel` | Normal: Drive | `inDir * min(C, s + Acceleration * dt)` |
  | `LinearBrakeModel` | Normal: Brake | `sign(velocity) * max(0, s - Friction * dt)` |
  | `LinearTurnModel` | Normal: Turn | With `next = velocity + inDir * TurnAcceleration * dt`: `inDir * C` when `sign(next) == inDir` and `abs(next) >= C`, otherwise `next` |

## ADSR

- **MOV-30** `Adsr` has `ControlledSpeed` and four slots: `Attack`, `Decay`, `Sustain`, `Release`.
- **MOV-31** The current ADSR state is kept in memory: Idle, Attack, Decay, Sustain or Release. Idle has no slot, and zero-valued memory means Idle.
- **MOV-32** Each `Calculate` runs these steps in order:
  1. Normalize speed (MOV-33).
  2. Select the state (MOV-34).
  3. Compute the new velocity (MOV-35).
  4. Check whether Attack has finished (MOV-36).
- **MOV-33** A speed within `0.001` of `C` is treated as exactly `C`. Otherwise, a speed within `0.001` of 0 is treated as exactly 0. Velocity passes through a rotation between frames in `RunState`, so exact comparison would drop out of Sustain.
- **MOV-34** State selection. The first matching row wins.

  | # | Current state | Input | Speed | New state |
  |---|---|---|---|---|
  | 1 | Attack | Held | Any | Attack |
  | 2 | Any | Any | `s > C` | Decay |
  | 3 | Any | Held | `s == C` | Sustain |
  | 4 | Any | Released | `s == 0` | Idle |
  | 5 | Any | Held | `s < C` | Attack |
  | 6 | Any | Released | `0 < s <= C` | Release |

  Row 1 is the only row that depends on the current state.
- **MOV-35** In Idle the new velocity is 0. In any other state the state's slot computes it with the target speed below.

  | State | Target speed |
  |---|---|
  | Attack | `C` |
  | Decay | `C` |
  | Sustain | `C` |
  | Release | `0` |

- **MOV-36** After Attack has computed the new velocity, the state becomes Decay if the new speed is at least `C` and the Attack model reports finished. Attack never ends below `C`.
- **MOV-37** Direction plays no part in state selection, so turning never changes the state. What a turn does to velocity is decided by the phase model.

### ADSR phase models

- **MOV-40** A phase model implements `IAdsrModel`: `Calculate(velocity, inDir, dt, targetSpeed)` returns the new velocity.
- **MOV-41** An Attack model implements `IAdsrAttackModel`, which adds `IsFinished(speed, targetSpeed)`. The model owns its limit.
- **MOV-42** A Decay or Release model must not take speed past `targetSpeed`, and it must reach it exactly.
- **MOV-43** A slot accepts a phase model only if the model allows the slot's state (CFG-10).

  | Slot | Interface | Default model |
  |---|---|---|
  | `Attack` | `IAdsrAttackModel` | `SimpleAttack` |
  | `Decay` | `IAdsrModel` | `LinearSlowdown` |
  | `Sustain` | `IAdsrModel` | `KeepSpeed` |
  | `Release` | `IAdsrModel` | `LinearSlowdown` |

- **MOV-44** ADSR phase models. `dir` is `inDir` when held, otherwise `sign(velocity)`, so a turn reverses direction at once and keeps speed.

  | Model type | Allowed states | Fields | Result |
  |---|---|---|---|
  | `SimpleAttack` | Attack | `Acceleration`, `PeakSpeed` | `dir * min(limit, s + Acceleration * dt)`, finished when `s >= limit`, where `limit = max(PeakSpeed, targetSpeed)` |
  | `LinearSlowdown` | Decay, Release | `Deceleration` | `dir * max(targetSpeed, s - Deceleration * dt)` |
  | `KeepSpeed` | Sustain | None | `dir * s` |

### ADSR examples

With a `SimpleAttack` whose limit is above `C`:

| Start | Input | States passed through |
|---|---|---|
| At rest | Held | Idle → Attack → Decay → Sustain |
| Attack, `s < C` | Released | Release → Idle |
| Attack, `s > C` | Released | Decay → Release → Idle |
| Decay | Released | Decay → Release → Idle |
| Sustain | Released | Release → Idle |
| Release | Held | Attack → Decay → Sustain |
| State entered with `s > C` | Held | Decay → Sustain |
| Sustain, `C` raised while the game runs | Held | Attack → Decay → Sustain |

## Verification

- MOV-03, MOV-05, MOV-06, MOV-10 to MOV-13, MOV-15, MOV-30 to MOV-42 and MOV-44: automated tests with xUnit v3 that call `Calculate` directly with a fixed `dt`.
- MOV-01, MOV-02, MOV-04, MOV-14 and MOV-43: manual check in the running game.

## Known gaps

None known.
