# Combat Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `SolsDawn.Core.Logic/Combat/`, and in `SolsDawn.Core.Logic/Entities/` the attack states in `PlayerStates.cs`, `PlayerController.cs`, `Player.cs`, `PlayerAnimations.cs`, `Enemy.cs`, `EnemyAnimations.cs`; the after phase of `LevelRunner.cs`, the debug category of `Collider` and the collider list in `Physics.cs`.

Requirements are `CMB-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Side | `Player` or `Enemy`. Every hurtbox and parrybox has one side. |
| Targets | The set of sides an attack can hit and parry. |
| Parry kind | `None`, `Blade` or `Fire`. An attack's parry kind selects the parryboxes it can parry. |
| Resolve | The once-per-frame step that tests overlaps and fires combat events. |
| Live | Created before the current Resolve started and not destroyed. |
| Owner job | The job an object was created in. Ending the job destroys the object. |
| Press frame | The one frame on which `Input.Blade`, `Input.Fire` or `Input.Jump` is set. |
| Center | `Player.Transform.Position`. |
| Pointer | `Camera.ScreenToWorld(ScreenPosition)` of the pressed input, taken on the press frame. |
| Aim | `normalize(pointer - center)`, fixed when Blade or Fire is entered. |
| Movement axis | The direction an attack or bounce state moves the player in. Its `Axis` model sets it (MOV-71, MOV-73). |
| Platform tangent | The unit vector `(cos a, sin a)`, where `a` is the platform's world rotation. |
| Platform change | As in the Movement spec: an update on ground whose platform differs from the previous update's, including landing and the first update on ground. |
| Tangent projection | Of a direction `d` onto a platform tangent `t`: `t` when `dot(d, t) >= 0`, otherwise `-t`. `CombatMath.TangentProjection` computes it for axis models. |
| Signal | A flag in `PlayerBoard` that a state raises for `PlayerController`: `ControlBack` or `Parried` (CMB-25). |
| `stateEnded` | The current state's job has ended. |
| Time since entry, `t` | `(n - 1) * dt` in the n-th update of a state's job, so 0 in the first update. |
| Pentagon hitbox | A convex pentagon with points `(0, H)`, `(Lh, H)`, `(Lh + T, 0)`, `(Lh, -H)`, `(0, -H)`, where `Lh` is `Length`, `H` is `HalfWidth` and `T` is `TipLength`. Its origin is the center and its X axis points along a given direction. |

## Combat objects

- **CMB-01** `Hurtbox` is the damageable area of an entity. It has a `Collider`, a `Side`, a `Hit` event stream and `LastAttack`, the attack of the latest hit.
- **CMB-02** `Parrybox` has a `Collider`, a `Side`, `Accepts` (`Blade` or `Fire`), a `Parried` event and `ParriedBy`, the attack that parried it.
- **CMB-03** `Attack` has a `Collider`, `Targets`, a parry kind, a `Hit` event stream, `LastTarget` (the hurtbox of the latest hit), a `Parried` event, `ParriedBoxes` (the parryboxes of its parry), `ParryPoint` (empty unless the attacker sets it) and an `AlreadyHit` set of hurtboxes.
- **CMB-04** Every combat object is created inside a job and is a resource of its owner job, so killing that job destroys it.
- **CMB-05** Combat colliders use layer `None`, so movement casts never see them.
- **CMB-06** Combat colliders are drawn by the collider debug draw with a category: `Default` for hurtboxes, `Parry` for parryboxes, `Attack` for attacks.
- **CMB-07** `Physics` adds and removes colliders lazily. A new collider joins the cast list at the start of the next outermost `CircleCast`. A destroyed collider is skipped at once and leaves the list at the start of the next outermost `CircleCast`. A cast started from inside another cast, for example from a filter, never changes the list.

## Resolve

- **CMB-10** Resolve runs once per frame in the after phase of the level runner (LVL-04), after the player and the level, including enemy behaviours, and before the camera update.
- **CMB-11** Two colliders overlap when `Collision.TestOverlap` on their shapes and current poses says so. Overlap is tested on every frame, not only on the first contact.
- **CMB-12** Objects created during Resolve take part from the next Resolve. An object destroyed during Resolve is skipped from that moment on.
- **CMB-13** Pass 1, parries. For each live attack with a parry kind other than `None` whose `Parried` has not fired, the parryboxes are every live parrybox whose `Parried` has not fired, whose side is in the attack's targets, whose `Accepts` equals the attack's parry kind, and which overlaps the attack. If there is at least one:
  1. `ParriedBoxes` is set to them, and `ParriedBy` of each is set to the attack.
  2. The attack's `Parried` fires.
  3. Each parrybox's `Parried` fires, in registration order.
- **CMB-14** One attack fires `Parried` once, however many parryboxes it parries. A parrybox fires `Parried` once. After firing, neither takes part in pass 1 again.
- **CMB-15** Pass 2, hits, starts after pass 1 has handled every attack. For each live attack and each live hurtbox whose side is in the attack's targets, which is not in `AlreadyHit` and which overlaps the attack:
  1. The hurtbox is added to `AlreadyHit`, `LastTarget` and `LastAttack` are set.
  2. The attack's `Hit` fires.
  3. The hurtbox's `Hit` fires.
- **CMB-16** An attack hits each hurtbox at most once in its lifetime.
- **CMB-17** Handlers run synchronously inside Resolve. Pass 2 uses each attack as pass 1 handlers left it: a handler that destroys the attack prevents its hits this frame; a handler that changes its shape changes what it hits.
- **CMB-18** Each side reacts only to its own objects: the attacker to its attack's events, the defender to its hurtbox's and parrybox's events.

## Player states

- **CMB-20** `AllowedState` gains `Blade`, `Fire`, `DownAttack` and `Bounce`. `PlayerSpecs` gains a state slot for each. Defaults follow CFG-25:

  | Slot | Default model |
  |---|---|
  | `Blade` | `BladeState` |
  | `Fire` | `FireState` |
  | `DownAttack` | `DownAttackState` |
  | `Bounce` | `BounceState` |

- **CMB-21** These models are `PlayerState`s and hold only configuration (MOV-05). Runtime values (pointer, phase, speed) are job locals or `PlayerBoard` fields.
- **CMB-22** `PlayerController` picks the state at the start of each frame, before the state's job runs. For the current state the first matching row wins. If no row matches, the state does not change. Terms in the conditions:

  | Term | Meaning |
  |---|---|
  | Ground, air | On ground, not on ground |
  | `I:run` | `Input.Move != 0` |
  | `I:jump` | `Input.Jump` |
  | `I:downA` | `Input.Jump` in air |
  | `I:blade`, `I:fire` | `Input.Blade`, `Input.Fire` with a valid pointer: farther than `0.001` from the center |
  | `V` | `PlayerBoard.Velocity`. `V = 0` means the zero vector. |
  | `controlBack`, `parried` | The signals `ControlBack` and `Parried` |

  | Current | # | Condition | New state |
  |---|---|---|---|
  | Idle | 1 | `I:blade` | Blade |
  | | 2 | `I:fire` | Fire |
  | | 3 | Ground, `I:jump` | Jump |
  | | 4 | Ground, `I:run` | Run |
  | | 5 | Air | Fall |
  | Run | 1 | `I:blade` | Blade |
  | | 2 | `I:fire` | Fire |
  | | 3 | Ground, `I:jump` | Jump |
  | | 4 | Ground, not `I:run`, `V = 0` | Idle |
  | | 5 | Air | Fall |
  | Jump | 1 | `I:blade` | Blade |
  | | 2 | `I:fire` | Fire |
  | | 3 | `I:downA` | DownAttack |
  | | 4 | Ground, `I:run` or `V != 0` | Run |
  | | 5 | Ground, `V = 0` | Idle |
  | | 6 | Air, `stateEnded` | Fall |
  | Fall | 1 | `I:blade` | Blade |
  | | 2 | `I:fire` | Fire |
  | | 3 | `I:downA` | DownAttack |
  | | 4 | Ground, `I:run` or `V != 0` | Run |
  | | 5 | Ground, `V = 0` | Idle |
  | Bounce | 1 | `I:blade` | Blade |
  | | 2 | `I:fire` | Fire |
  | | 3 | Ground, `I:run` or `V != 0` | Run |
  | | 4 | Ground, `V = 0` | Idle |
  | | 5 | Air, `stateEnded` | Fall |
  | Blade | 1 | `controlBack`, `I:fire` | Fire |
  | | 2 | `controlBack`, `I:downA` | DownAttack |
  | | 3 | Ground, (`controlBack` and `I:run`) or (`stateEnded` and `V != 0`) | Run |
  | | 4 | Ground, `stateEnded`, `V = 0` | Idle |
  | | 5 | Air, `stateEnded` | Fall |
  | Fire | 1 | `controlBack`, `I:blade` | Blade |
  | | 2 | `controlBack`, `I:downA` | DownAttack |
  | | 3 | Ground, (`controlBack` and `I:run`) or (`stateEnded` and `V != 0`) | Run |
  | | 4 | Ground, `stateEnded`, `V = 0` | Idle |
  | | 5 | Air, `stateEnded` | Fall |
  | DownAttack | 1 | `parried` | Bounce |
  | | 2 | Ground, `I:run` or `V != 0` | Run |
  | | 3 | Ground, `V = 0` | Idle |

- **CMB-23** A press that causes no transition is dropped. There is no input buffer and no cooldown.
- **CMB-24** The pointer of `I:blade` and `I:fire` is stored in `PlayerBoard` when the state is entered.
- **CMB-25** `PlayerBoard` has the signals `ControlBack` and `Parried`. `PlayerController` clears both in one place, whenever it enters a state. A state raises a signal in the same update, or the same Resolve handler, in which its condition happens, never one frame later.
- **CMB-26** Only `PlayerController` changes state because of ground, velocity or input. A state's job never ends itself on these conditions. A job ends itself only by its own timeline, such as an attack state reaching its `PeakSpeed` (CMB-32) or Jump reaching its `Duration` (MOV-82).
- **CMB-27** A state's job runs its first update in the frame in which the state is entered. A signal raised, or a job ended, in frame `n` changes the state at the start of frame `n + 1`.

## Attack movement

Blade, Fire and Bounce move the same way: a model sets the movement axis and a speed, then an A phase model raises the speed to `PeakSpeed` along that axis.

- **CMB-28** An attack motion keeps the movement axis and the speed as Job locals. Each update, in order:
  1. On entry, and in Blade and Fire also on a platform change, the state's `Axis` model sets the movement axis and the speed.
  2. If `speed < PeakSpeed`, `speed = Movement.Calculate(dt, speed, 0, PeakSpeed)`. Otherwise speed does not change.
  3. Velocity is `movement axis * speed`.
  4. When `speed >= PeakSpeed`, the job ends in this update, after the state's other steps. The velocity stays as step 3 set it.
- **CMB-29** Gravity does not act during these states. Their slots are read on every update, so a model swapped while the game runs takes effect at once (CFG-67).

## BladeState

- **CMB-30** `BladeState` has `PeakSpeed`, an `Axis` slot of category `Aim axis` for `Blade` (default by CFG-25), a `Movement` slot that accepts A phase models (default by CFG-25), `Length`, `HalfWidth`, `TipLength`, `HitboxStart`, `HitboxEnd` and `ControlBackTime`.
- **CMB-31** The `Axis` call is `Axis.Calculate(velocity, aim, tangent)`, with the current platform's tangent on ground and the zero vector in the air. In the air the movement axis keeps its direction, so leaving a platform does not turn it.
- **CMB-32** Each update, in order:
  1. CMB-28 steps 1 to 3.
  2. While `HitboxStart <= t < HitboxEnd`, an attack exists: a pentagon hitbox along the movement axis of the current update, moving with the player, targets `Enemy`, parry kind `Blade`.
  3. In the first update with `t >= ControlBackTime`, `ControlBack` is raised.
  4. CMB-28 step 4.
- **CMB-34** When the attack's `Parried` fires, the attack is destroyed at once. Movement does not change.

## FireState

- **CMB-40** `FireState` has `PeakSpeed`, an `Axis` slot of category `Aim axis` for `Fire` (default by CFG-25), a `Movement` slot that accepts A phase models (default by CFG-25), `ControlBackTime`, `Distance`, `Width`, `TraceDuration`, `TraceColor`, `ExplosionRadius`, `ExplosionDuration` and `ExplosionColor`.
- **CMB-41** On entry the job creates an attack: a rectangle `Distance` long and `Width` wide, centred at `center + aim * Distance / 2` and rotated to the aim, targets `Enemy`, parry kind `Fire`. The attack takes part in exactly one Resolve, then it is destroyed. Its owner job is the `Effects` job of the level runner, not the fire state job, so leaving Fire early does not cancel the shot or its trace. In CMB-41 to CMB-44, `center` is the center at entry, and the shot does not follow the player.
- **CMB-42** The player moves by CMB-28 and CMB-31 with the Fire `Axis` slot. The shot always uses the aim, whatever axis the model returns. `ControlBack` is raised as in CMB-32 step 3.
- **CMB-43** When the attack's `Parried` fires:
  1. `t = clamp(min over ParriedBoxes of dot(box position - center, aim) / 2, 0, Distance)`. The parry point is `center + aim * t`, so it lies on the aim.
  2. The attack's `ParryPoint` is set to it.
  3. The attack becomes a rectangle `t` long and `Width` wide from the center to the parry point. It still hits in pass 2.
  4. An explosion starts at the parry point (CMB-45).
- **CMB-44** A trace line of `Width` goes from the center to the far end of the attack and fades out over `TraceDuration`.
- **CMB-45** The explosion is an attack: a circle of `ExplosionRadius`, targets `Player` and `Enemy`, parry kind `None`. Its owner job is the `Effects` job of the level runner, not the fire state job. It takes part in exactly one Resolve. A filled circle of `ExplosionColor` fades out over `ExplosionDuration`.

## DownAttackState and BounceState

- **CMB-50** `DownAttackState` has a `Vertical` slot of category `Envelope` (default `Ads`), a `Horizontal` slot that accepts R phase models (default by CFG-25), `Length`, `HalfWidth` and `TipLength`.
- **CMB-51** On entry the job takes `h = |V.x|`, `h0 = h` and `hDir = sign(V.x)`, and creates an attack for the whole state: a pentagon hitbox moving with the player, targets `Enemy`, parry kind `Blade`.
- **CMB-52** Each update, in order:
  1. `vd = Vertical.Calculate(-V.y, 1, dt, ref memory)`, along Y down, so an upward velocity first runs the turn (MOV-53).
  2. If `h > 0`, `h = Horizontal.Calculate(dt, h, h0, 0)`.
  3. Velocity is `(hDir * h, -vd)`.
  4. The hitbox points along the velocity, or straight down when the velocity is zero.

  The job never ends by itself. `PlayerController` leaves the state on ground or on a parry (CMB-22).
- **CMB-53** When the attack's `Parried` fires, the attack is destroyed at once and `Parried` is raised. Movement does not change.
- **CMB-54** `BounceState` has `PeakSpeed`, an `Axis` slot of category `Bounce axis` (default by CFG-25) and a `Movement` slot that accepts A phase models (default by CFG-25). It moves by CMB-28 and CMB-29, with the movement axis and speed from `Axis.Calculate(velocity)` on entry only.

## Hits and visuals

- **CMB-60** There is no health. When a hurtbox's `Hit` fires, its entity blinks `HitBlinkColor` for `HitDuration`. The player has these fields in `PlayerSpecs`, the enemy in `EnemySpecs`.
- **CMB-61** The player is drawn as a filled circle of `ColliderRadius` in `Color`. `PlayerSpecs` has no `Width` or `Height`.
- **CMB-62** The player hurtbox is a circle of `ColliderRadius`, side `Player`. It follows a change of `ColliderRadius` while the game runs.
- **CMB-63** Traces, blinks and fades are visuals that do not affect Resolve.

## Enemy

- **CMB-70** The enemy body never moves by itself. Nothing collides with it.
- **CMB-71** `EnemySpecs` is a model type in the `Enemy` category. A level passes an `EnemySpecs` slot to `Spawn.Enemy` (LVL-11).

  | Group | Fields |
  |---|---|
  | Body | `Color`, `Width`, `Height`, `HitDuration`, `HitBlinkColor`, `AttackGap` |
  | Blade | `BladeTelegraphDuration`, `BladeTelegraphColor`, `BladeLength`, `BladeWidth`, `BladeAttackDuration`, `BladeAttackColor`, `BladeParriedDuration`, `BladeParriedColor` |
  | Fire | `FireTelegraphDuration`, `FireAimingDuration`, `FireTelegraphColor`, `FireDistance`, `FireWidth`, `FireTraceDuration`, `FireTraceColor`, `FireParriedDuration`, `FireParriedColor` |

- **CMB-72** The enemy is drawn as a filled rectangle `Width × Height` in `EnemyBoard.Tint`, or in `Color` when there is no tint. Its hurtbox has the same rectangle, side `Enemy`. It follows a change of `Width` or `Height` while the game runs.
- **CMB-73** What the enemy does is its behaviour, a job given to `Spawn.Enemy` (LVL-11). Behaviours are designer content in `SolsDawn.Gameplay` and use the combat objects of CMB-01 to CMB-03 and `EnemyBoard.Tint`. The Blade and Fire fields of `EnemySpecs` are for them.
- **CMB-76** A hit blink (CMB-60) shows over the tint.

## Verification

- CMB-43 step 1 and the tangent projection: automated tests with xUnit v3 on `CombatMath`.
- CMB-11 to CMB-17: automated tests with xUnit v3 that build attacks, parryboxes and hurtboxes inside a job and call Resolve directly.
- CMB-01 to CMB-07, CMB-10, CMB-18, CMB-20 to CMB-32, CMB-34, CMB-40 to CMB-42, CMB-43 steps 2 to 4, CMB-44, CMB-45, CMB-50 to CMB-54, CMB-60 to CMB-63, CMB-70 to CMB-73 and CMB-76: manual check in the running game.

## Known gaps

- A fast attack can pass over a thin parrybox between two frames. At 60 fps a 1-unit parrybox is skipped only above about 60 units/s.
- Input buffering and attack cooldowns are in the backlog.
- Visual lifetimes (traces, fades) do not follow job ownership; this is in the backlog.
