# Movement Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `Movement.cs`, `Adsr.cs`, `PlayerStates.cs`. Phase model types are designer content in `SolsDawn.Gameplay`; this spec covers only the contract they follow (MOV-37 to MOV-41).

Requirements are `MOV-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Movement model | A model that computes velocity along one axis, once per frame, for a `PlayerState`: `Adsr`, `AR` or `AS`. |
| Phase model | A model in a slot of a movement model. It computes velocity for one ADSR phase. |
| Velocity | Signed speed along the movement axis: the platform tangent in `RunState`, X in the air, the attack axis in attack states. |
| Speed, `s` | The absolute value of velocity. |
| `Sign` | A value that is always -1, 0 or 1. Any number converts to its sign implicitly, and `Sign` converts to `int` implicitly. It is defined in `SolsDawn.Core.Logic` and has no spec. |
| `inDir` | Input direction, a `Sign`. |
| `prevInDir` | The `inDir` a phase model saw on the previous frame of the same phase (MOV-38). |
| Held, released | `inDir != 0`, `inDir == 0`. |
| `C` | The `ControlledSpeed` of `Adsr`. |
| `P` | The `PeakSpeed` of `Adsr`, `AR` or `AS`. |
| Memory | A `MovementMemory` value: the state of a movement model between frames of one Job. It holds a phase and the previous `inDir`. |
| A / D / S / R | Attack, Decay, Sustain, Release. |

## Movement models

- **MOV-01** `Adsr` is a model type with the `Movement` category. `AR` and `AS` have categories of their own (MOV-50). All three implement `IMovementModel`.
- **MOV-02** Each `PlayerState` that moves under player control has one slot of category `Movement`. The slot accepts `Adsr`, which is also its default model. Attack states use `AR` and `AS` slots instead (CMB-30, CMB-40, CMB-50).

  | Model type | Slot |
  |---|---|
  | `RunState` | `Movement` |
  | `CelesteJumpState` | `HorizontalMovement` |
  | `PrimitiveJumpState` | `HorizontalMovement` |
  | `FallState` | `HorizontalMovement` |
  | `BounceState` | `HorizontalMovement` |

- **MOV-03** `IMovementModel` has one operation: `Calculate(velocity, inDir, dt, ref memory)` with `inDir` a `Sign` advances one frame, updates `memory` and returns the new velocity.
- **MOV-04** A `PlayerState` Job creates one zero-valued memory when it starts, keeps it as a local value and passes it to every `Calculate`. It reads the slot's active model on every frame, so a model swapped while the game runs takes effect at once (CFG-67).
- **MOV-05** Movement models and phase models hold only configuration. All movement runtime state is in the memory, which is never saved and never shared between Jobs, so any number of slots and entities may reference the same model.
- **MOV-06** A movement model accepts any memory value on any frame, including one written by another movement model type before a swap.
- **MOV-07** States read input as `Input.Move`, which is a `Sign`: 1 for right, -1 for left, 0 for none or both.

## ADSR

- **MOV-30** `Adsr` has `ControlledSpeed`, `PeakSpeed` and four slots: `Attack`, `Decay`, `Sustain`, `Release`.
- **MOV-31** The memory keeps the ADSR phase that ran on the previous frame: Idle, Attack, Decay, Sustain or Release. Idle has no slot, and zero-valued memory means Idle.
- **MOV-32** Each `Calculate` runs these steps in order:
  1. Normalize speed (MOV-33).
  2. Select the phase (MOV-34).
  3. Compute the new velocity (MOV-35).
  4. Store the phase and `inDir` in memory.
- **MOV-33** A speed within `0.001` of `C` is treated as exactly `C`. Otherwise, a speed within `0.001` of 0 is treated as exactly 0. Velocity passes through a rotation between frames in `RunState`, so exact comparison would drop out of Sustain.
- **MOV-34** Phase selection, with `L = max(P, C)`. The first matching row wins.

  | # | Previous phase | Input | Speed | New phase |
  |---|---|---|---|---|
  | 1 | Attack | Held | `s < L` | Attack |
  | 2 | Any | Any | `s > C` | Decay |
  | 3 | Any | Held | `s == C` | Sustain |
  | 4 | Any | Released | `s == 0` | Idle |
  | 5 | Any | Held | `s < C` | Attack |
  | 6 | Any | Released | `0 < s <= C` | Release |

  Row 1 is the only row that depends on the previous phase. Attack runs until speed reaches `L`, so it never ends below `C`.
- **MOV-35** In Idle the new velocity is 0. In any other phase the phase's slot computes it with the target speed below.

  | Phase | Target speed |
  |---|---|
  | Attack | `L` |
  | Decay | `C` |
  | Sustain | `C` |
  | Release | `0` |

- **MOV-36** Direction plays no part in phase selection, so turning never changes the phase. What a turn does to velocity is decided by the phase model.

### Phase models

- **MOV-37** A phase model implements `IAdsrModel`: `Calculate(velocity, prevInDir, inDir, dt, targetSpeed)` returns the new velocity. `prevInDir` and `inDir` are `Sign`s. The result depends only on the arguments.
- **MOV-38** On the first frame of a phase, `prevInDir` equals `inDir`. On later frames of the same phase it is the `inDir` of the previous frame. A phase starts when the phase that runs differs from the phase that ran on the previous frame, and on the first frame of a Job.
- **MOV-39** An Attack, Decay or Release model moves speed toward `targetSpeed`, never past it, and reaches it exactly. Callers call Attack only with `s < targetSpeed`.
- **MOV-40** A slot accepts a phase model only if the model allows the slot's phase (CFG-10). This holds for the slots of `Adsr`, `AR`, `AS` and attack states. A slot's default phase model follows CFG-25.
- **MOV-41** Phase model types are defined in `SolsDawn.Gameplay`. Every phase required by a slot in this spec needs at least one phase model type that allows it, otherwise the Library fails to initialize (CFG-26).

### ADSR examples

With `P > C`:

| Start | Input | Phases passed through |
|---|---|---|
| At rest | Held | Idle → Attack → Decay → Sustain |
| Attack, `s < C` | Released | Release → Idle |
| Attack, `s > C` | Released | Decay → Release → Idle |
| Decay | Released | Decay → Release → Idle |
| Sustain | Released | Release → Idle |
| Release | Held | Attack → Decay → Sustain |
| Phase entered with `s > C` | Held | Decay → Sustain |
| Sustain, `C` raised while the game runs | Held | Attack → Decay → Sustain |

## Envelopes

An envelope is a fixed sequence of ADSR phases that runs once. It ignores whether input is held: the caller passes `inDir` only to give the phase models a direction.

- **MOV-50** `AR` and `AS` are model types. `AR` has the category `Envelope AR` and `AS` the category `Envelope AS`, so a slot of one accepts only that envelope.
- **MOV-51** `AR` has `PeakSpeed` and two slots: `Attack` and `Release`. Its memory phase is Start, Attack, Release or Done. Zero-valued memory means Start: the envelope has not run yet. Start is selected like Attack but is a different phase, so the first frame starts a phase (MOV-38).
- **MOV-52** `AR` phase selection, before the velocity is computed:

  | # | Previous phase | Speed | Phase |
  |---|---|---|---|
  | 1 | Start or Attack | `s < P` | Attack, target `P` |
  | 2 | Start, Attack or Release | Any | Release, target 0 |
  | 3 | Otherwise | Any | Done |

  When Release computes speed 0, the phase becomes Done on the same frame. In Done the velocity is 0.
- **MOV-53** `AR` reports Done through `IsDone(memory)`. A state that ends on Done ends in the same update in which `Calculate` reached it.
- **MOV-54** `AS` has `PeakSpeed` and two slots: `Attack` and `Sustain`. Its memory phase is Start, Attack or Sustain. Zero-valued memory means Start, as in MOV-51.
- **MOV-55** `AS` phase selection: Attack with target `P` while the previous phase is Start or Attack and `s < P`, otherwise Sustain with target `P`. `AS` never ends by itself.
- **MOV-56** Envelopes follow MOV-37 to MOV-39: they pass `prevInDir` by MOV-38 and store the phase and `inDir` in memory.
- **MOV-57** An envelope entered with `s >= P` skips Attack at once: `AR` starts in Release, `AS` in Sustain.

## Verification

- MOV-03, MOV-05, MOV-06, MOV-30 to MOV-38 and MOV-51 to MOV-57: automated tests with xUnit v3 that call `Calculate` directly with a fixed `dt`, using phase models defined in the test project.
- MOV-01, MOV-02, MOV-04, MOV-07, MOV-39, MOV-40, MOV-41 and MOV-50: manual check in the running game.

## Known gaps

None known.
