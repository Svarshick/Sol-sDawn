# Movement Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `Movement.cs`, `Adsr.cs`, `PlayerStates.cs`. Phase, projection and axis model types are designer content in `SolsDawn.Gameplay`; this spec covers only the contracts they follow (MOV-60 to MOV-75). The attack and bounce states are in the Combat spec (CMB-30 to CMB-54).

Requirements are `MOV-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Movement model | A model that computes velocity along one axis, once per frame: `Adsr` or `Ads`. |
| Phase model | A model in a phase slot. It computes speed for one phase. |
| Velocity | Signed speed along the movement axis: the platform tangent in `RunState`, the jump axis in `JumpState`, the movement axis in `BladeState` and `FireState`, X or Y in the air. |
| Speed, `s` | The absolute value of velocity. |
| Motion direction, `m` | The sign of the velocity passed to a movement model. |
| `Sign` | A value that is always -1, 0 or 1. Any number converts to its sign implicitly, and `Sign` converts to `int` implicitly. It is defined in `SolsDawn.Engine` and has no spec. |
| `inputDir` | Input direction, a `Sign`. |
| Input class | None, Along or Against (MOV-35). |
| `C`, `P` | The `ControlledSpeed` and `PeakSpeed` of a movement model. |
| Edges | `fromSpeed` and `toSpeed`: the speeds a phase moves between (MOV-37). |
| Phase | Idle, A, D, S, R, TD, TR or TA. A, D, S, R are Attack, Decay, Sustain, Release. |
| Turn | The phases TD, TR and TA: Decay, Release and Attack of a `Dra`. |
| Memory | A `MovementMemory` value: the phase a movement model ran on the previous frame of one Job. |
| Platform tangent | The unit vector `(cos a, sin a)`, where `a` is the platform's world rotation. |
| Platform normal | The platform tangent rotated by 90° counterclockwise: the side the player stands on. |
| Platform change | An update of a state's Job in which the player is on ground and the current platform differs from the one in the previous update of that Job, or the first update on ground of that Job. Landing counts, since the platform before was none. |

## Movement models

- **MOV-01** `Adsr` is a model type with the `Movement` category, `Ads` with the `Envelope` category. Both implement `IMovementModel`. A slot of one category never accepts the other, so `Ads` is never used where input can be released.
- **MOV-02** Movement slots of player states. `Adsr` and `Ads` are the only candidates of their categories, so they are the defaults.

  | Model type | Slot | Category |
  |---|---|---|
  | `RunState` | `Movement` | `Movement` |
  | `JumpState` | `Vertical/Movement` | `Envelope` |
  | `JumpState` | `Horizontal/Movement` | `Envelope` |
  | `FallState` | `Horizontal` | `Movement` |
  | `FallState` | `Vertical` | `Envelope` |
  | `DownAttackState` | `Vertical` | `Envelope` |
  | `BladeState` | `Movement` | `Envelope` |
  | `FireState` | `Movement` | `Envelope` |

- **MOV-03** `IMovementModel` has one operation: `Calculate(velocity, inputDir, dt, ref memory)` with `inputDir` a `Sign` advances one frame, updates `memory` and returns the new velocity.
- **MOV-04** A `PlayerState` Job creates one zero-valued memory per movement slot when it starts, keeps it as a local value and passes it to every `Calculate` of that slot. It reads the slot's active model on every frame, so a model swapped while the game runs takes effect at once (CFG-67).
- **MOV-05** Movement models, `Dra` and phase models hold only configuration. All movement runtime state is in the memory, which is never saved and never shared between Jobs, so any number of slots and entities may reference the same model.
- **MOV-06** A movement model accepts any memory value on any frame, including one written by the other movement model type before a swap. A phase value it does not know is treated as Idle.
- **MOV-07** States read input as `Input.Move`, which is a `Sign`: 1 for right, -1 for left, 0 for none or both.

## ADSR

- **MOV-30** `Adsr` has `ControlledSpeed`, `PeakSpeed`, the phase slots `Attack`, `Decay` and `Release`, and a `Turn` slot that holds a `Dra` (MOV-40).
- **MOV-31** `P >= C` always. `Adsr` keeps it through CFG-66: an edit that sets `PeakSpeed` below `C` lowers `ControlledSpeed` to it, and an edit of `ControlledSpeed` above `P`, or a load with `P < C`, raises `PeakSpeed` to `C`.
- **MOV-32** The memory keeps the phase that ran on the previous frame. Zero-valued memory means Idle. Idle and S have no slot.
- **MOV-33** Each `Calculate` runs these steps in order:
  1. Normalize speed (MOV-34).
  2. Classify input (MOV-35).
  3. Select the phase (MOV-36).
  4. Compute the new speed (MOV-37).
  5. Give it a direction (MOV-38).
  6. Store the phase in memory.
- **MOV-34** A speed within `0.001` of `C` is treated as exactly `C`. Otherwise, a speed within `0.001` of 0 is treated as exactly 0. This keeps float error from dropping out of S.
- **MOV-35** Input classes, measured against the motion direction `m` of the velocity passed in:

  | Class | When |
  |---|---|
  | None | `inputDir = 0` |
  | Along | `inputDir != 0`, and `s = 0` or `inputDir = m` |
  | Against | `inputDir != 0`, `s > 0` and `inputDir = -m` |

  At `s = 0` there is no motion to be against, so every held input is Along.
- **MOV-36** Phase selection. The first matching row wins. A row whose new phase equals the previous one continues that phase.

  | # | Previous phase | Input | Speed | New phase |
  |---|---|---|---|---|
  | 1 | TA | Along | `s < P` | TA |
  | 2 | Any | Against | `s > C` | TD |
  | 3 | Any | Against | `s <= C` | TR |
  | 4 | TD, TR | Along | `s = 0` | TA |
  | 5 | A | Along | `s < P` | A |
  | 6 | Any | Along, None | `s > C` | D |
  | 7 | Any | Along | `s = C` | S |
  | 8 | Any | None | `s = 0` | Idle |
  | 9 | Any | Along | `s < C` | A |
  | 10 | Any | None | `0 < s <= C` | R |

  Rows 1 to 4 are the turn: TD and TR brake the old motion, row 4 hands a turn that reached 0 to its own Attack, and TA accelerates in the new direction until `P`. Row 4 lists TD only for `C = 0`, where TR never runs.
- **MOV-37** In Idle the new speed is 0. In S it is the speed unchanged. In any other phase the phase's slot computes it from the edges below.

  | Phase | Slot | `fromSpeed` | `toSpeed` |
  |---|---|---|---|
  | A | `Attack` | 0 | `P` |
  | D | `Decay` | `P` | `C` |
  | R | `Release` | `C` | 0 |
  | TD | `Turn` → `Decay` | `P` | `C` |
  | TR | `Turn` → `Release` | `C` | 0 |
  | TA | `Turn` → `Attack` | 0 | `P` |

- **MOV-38** The new velocity is `d * speed`, where `d` is `m` when `s > 0` and `inputDir` when `s = 0`. So TD and TR brake in the old direction and TA starts the new one. Phase models never see a direction.
- **MOV-39** A turn takes at least two frames, even with phase models that reach their edge at once: the last frame of TR ends at speed 0, and TA follows. Such a turn stops the player for one frame and passes through `P` before D brings speed back to `C`. The speed the player had before the turn is not kept.

### ADSR examples

| Start | Input | Phases passed through |
|---|---|---|
| At rest | Held | Idle → A → D → S |
| A, `s < C` | Released | R → Idle |
| A, `s > C` | Released | D → R → Idle |
| S | Released | R → Idle |
| R | Held along | A → D → S |
| Entered with `s > C` | Held along | D → S |
| A, `s > C` | Reversed | TD → TR → TA → D → S |
| S | Reversed | TR → TA → D → S |
| TD or TR | Back to the old direction | D, S or A by speed, then on to S |
| TR | Released | R → Idle |
| TA | Reversed again | TD or TR: a new turn |
| TA | Released | D or R by speed |

### Turn

- **MOV-40** `Dra` is a model type with the `Turn` category. It has the phase slots `Decay`, `Release` and `Attack` and no speeds: its edges are those of the movement model that holds it (MOV-37, MOV-51).

## ADS

- **MOV-50** `Ads` has `PeakSpeed`, the phase slots `Attack` and `Decay`, and a `Turn` slot that holds a `Dra`. It has no Release and no `ControlledSpeed`.
- **MOV-51** `Ads` runs MOV-33 to MOV-38 with `C = P`. So A and TA have edges `(0, P)`, D and TD `(P, P)`, and TR `(P, 0)`.
- **MOV-52** Callers pass a fixed non-zero `inputDir`, the direction the envelope pushes toward. `Ads` treats `inputDir = 0` as `m`, so the input is Along, or None at `s = 0`, where the speed stays 0. Rows 8 and 10 of MOV-36 never select R.
- **MOV-53** Moving against the direction runs the turn: TD while `s > P`, TR down to 0, then TA up to `P`, then S. This is how Fall turns an upward velocity into a fall.

## Phase models

- **MOV-60** A phase model implements `IAdsrModel`: `Calculate(dt, speed, fromSpeed, toSpeed)` returns the new speed, at least 0. The result depends only on the arguments.
- **MOV-61** A phase model moves speed toward `toSpeed`, never past it, and reaches it exactly. A runs only with `speed < toSpeed`. D and TD may run with `speed > fromSpeed`, when a state is entered faster than `P`, and in `Ads` their `fromSpeed` equals `toSpeed`; the model decides how to handle this.
- **MOV-62** `AllowedAdsrState` has `Attack`, `Decay` and `Release`. A phase slot accepts a phase model only if the model allows the slot's phase (CFG-10). The slots of `Dra` use the same phases as the slots of `Adsr`. A slot's default phase model follows CFG-25.
- **MOV-63** Phase model types are defined in `SolsDawn.Gameplay`. Every phase required by a slot in this spec or the Combat spec needs at least one phase model type that allows it, otherwise the Library fails to initialize (CFG-26).

## Projection and axis models

- **MOV-70** A projection model has the `Projection` category and implements `IProjectionModel`: `Calculate(velocity, tangent)` with `velocity` a vector and `tangent` a unit axis returns the signed velocity along `tangent`. The axis is the platform tangent in `RunState` and the jump axis in `JumpState`.
- **MOV-71** An aim axis model has the `Aim axis` category with the states it allows, `Blade`, `Fire` or both, and implements `IAimAxisModel`: `Calculate(velocity, aim, tangent)` returns `(Axis, Speed)`. `aim` is a unit vector, `tangent` is the platform tangent or the zero vector in the air. `Axis` is a unit vector and `Speed` is at least 0.
- **MOV-72** A slot of category `Aim axis` with a state accepts only aim axis models that allow that state (CFG-10).
- **MOV-73** An axis model has the `Axis` category and implements `IAxisModel`: `Calculate(velocity)` returns `(Axis, Speed)`, with the same ranges as MOV-71.
- **MOV-74** These models hold only configuration, and their results depend only on the arguments.
- **MOV-75** Their types are defined in `SolsDawn.Gameplay`. Every slot of these categories needs at least one candidate (CFG-26).

## Player states

`t` is the time since entry: `(n - 1) * dt` in the n-th update of a state's Job.

- **MOV-80** `IdleState` sets the velocity to zero on entry. Its Job ends after the first update.
- **MOV-81** `RunState` has a `Projection` slot (category `Projection`) and a `Movement` slot. It keeps the velocity along the platform `v` as a Job local. Each update, in order:
  1. On a platform change, `v = Projection.Calculate(velocity, tangent)` with the current platform's tangent.
  2. `v = Movement.Calculate(v, Input.Move, dt, ref memory)`.
  3. Velocity is `v * tangent`.

  The Job never ends by itself. Running without a platform logs an error and ends the Job.
- **MOV-82** `JumpState` has two containers (CFG-04): `Vertical` and `Horizontal`. Each holds `Duration`, a `StartSpeed` slot (category `Projection`) and a `Movement` slot (category `Envelope`). `Horizontal` also holds `Angle`, in degrees from 0 to 90. On entry the state reads the platform tangent `T`, the platform normal `N` and `inputDir = Input.Move`, and picks the kind of jump once:

  | Kind | When | Container | Jump axis `A` |
  |---|---|---|---|
  | Vertical | `inputDir = 0` | `Vertical` | `N` |
  | Horizontal | `inputDir != 0` | `Horizontal` | `inputDir * T` rotated toward `N` by `Angle` |

  Then `v = StartSpeed.Calculate(velocity, A)` from the picked container. Each update, in order:
  1. `v = Movement.Calculate(v, 1, dt, ref memory)` from the picked container.
  2. Velocity is `v * A`.
  3. In the first update with `t >= Duration`, the Job ends, where `Duration` is the picked container's. It is read on every update.

  At `Angle = 90` a horizontal jump moves along `N`, like a vertical one, with its own container.
- **MOV-83** `PlayerController` leaves Jump on ground (CMB-22). Unless the first update lifts the player clear of the ground along `N`, the next frame enters Run and the jump is lost. At 60 fps this needs a first speed along `N` above about 0.3, for example `SimpleAttack` with `Acceleration` above about 18 from speed 0. A horizontal jump with `Angle = 0` never leaves the ground.
- **MOV-84** `FallState` has a `Horizontal` slot and a `Vertical` slot. Each update, in order:
  1. `vx = Horizontal.Calculate(vx, Input.Move, dt, ref horizontalMemory)`.
  2. `vd = Vertical.Calculate(-vy, 1, dt, ref verticalMemory)`, along Y down.
  3. Velocity is `(vx, -vd)`.

  The Job never ends by itself.
- **MOV-85** `FallState` on a platform and `JumpState` entered without a platform log an error and end their Job.

## Verification

- MOV-03, MOV-05, MOV-06, MOV-30 to MOV-39 and MOV-50 to MOV-53: automated tests with xUnit v3 that call `Calculate` directly with a fixed `dt`, using phase models defined in the test project.
- MOV-31 through CFG-66: automated tests with xUnit v3.
- MOV-01, MOV-02, MOV-04, MOV-07, MOV-40, MOV-60 to MOV-63, MOV-70 to MOV-75 and MOV-80 to MOV-85: manual check in the running game.

## Known gaps

None known.
