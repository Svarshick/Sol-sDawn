# Combat Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `SolsDawn.Gameplay/Combat/`, the attack states in `PlayerStates.cs`, `PlayerController.cs`, `Player.cs`, `PlayerAnimations.cs`, `Enemy.cs`, `EnemyAnimations.cs`, the combat part of `GameLoop.cs`, the debug category of `Collider` and the collider list in `Physics.cs`.

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
| Axis | `normalize(pointer - center)`, fixed when an attack state is entered. |
| Movement cast hit | `PlayerController`'s movement `CircleCast` stopping the player at a platform. The cast ignores the platform the player stands on, so a hit is always a new platform: another one while on ground, or any one from the air. |

## Combat objects

- **CMB-01** `Hurtbox` is the damageable area of an entity. It has a `Collider`, a `Side`, a `Hit` event stream and `LastAttack`, the attack of the latest hit.
- **CMB-02** `Parrybox` has a `Collider`, a `Side`, `Accepts` (`Blade` or `Fire`), a `Parried` event and `ParriedBy`, the attack that parried it.
- **CMB-03** `Attack` has a `Collider`, `Targets`, a parry kind, a `Hit` event stream, `LastTarget` (the hurtbox of the latest hit), a `Parried` event, `ParriedBoxes` (the parryboxes of its parry), `ParryPoint` (empty unless the attacker sets it) and an `AlreadyHit` set of hurtboxes.
- **CMB-04** Every combat object is created inside a job and is a resource of its owner job, so killing that job destroys it.
- **CMB-05** Combat colliders use layer `None`, so movement casts never see them.
- **CMB-06** Combat colliders are drawn by the collider debug draw with a category: `Default` for hurtboxes, `Parry` for parryboxes, `Attack` for attacks.
- **CMB-07** `Physics` adds and removes colliders lazily. A new collider joins the cast list at the start of the next outermost `CircleCast`. A destroyed collider is skipped at once and leaves the list at the start of the next outermost `CircleCast`. A cast started from inside another cast, for example from a filter, never changes the list.

## Resolve

- **CMB-10** Resolve runs once per frame in a top-level `GameLoop` job, after the player and enemy updates and before the camera update.
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

- **CMB-20** `AllowedState` gains `Blade`, `Fire`, `DownAttack` and `Bounce`. `PlayerSpecs` gains a state slot for each:

  | Slot | Default model |
  |---|---|
  | `Blade` | `SlideBlade` |
  | `Fire` | `SimpleFire` |
  | `DownAttack` | `SimpleDownAttack` |
  | `Bounce` | `BounceState` |

- **CMB-21** These models are `PlayerState`s and hold only configuration (MOV-05). Runtime values (pointer, phase, speed) are job locals or `PlayerBoard` fields.
- **CMB-22** `PlayerController` picks the state at the start of each frame. "In air" means not on ground. "Valid pointer" means the pointer is farther than `0.001` from the center. The first matching row wins.

  | # | Current state | Condition | New state |
  |---|---|---|---|
  | 1 | Jump, Fall, Bounce, Blade | In air, `Input.Jump` | DownAttack |
  | 2 | Idle, Run, Jump, Fall, Bounce | `Input.Blade`, valid pointer | Blade |
  | 3 | Idle, Run, Jump, Fall, Bounce | `Input.Fire`, valid pointer | Fire |
  | 4 | Blade, Fire, DownAttack | Job running | No change |
  | 5 | DownAttack | Job ended by a parry | Bounce |
  | 6 | Blade, Fire, DownAttack | Job ended, on ground, zero velocity, no move input | Idle |
  | 7 | Blade, Fire, DownAttack | Job ended, on ground | Run |
  | 8 | Blade, Fire, DownAttack | Job ended, in air | Fall |
  | 9 | Bounce | On ground | As rows 6 and 7 |
  | 10 | Bounce | `Velocity.Y <= 0` | Fall |

  Rows for Idle, Run, Jump and Fall that existed before this spec apply after these rows and are unchanged.
- **CMB-23** A press that causes no transition is dropped. There is no input buffer and no cooldown.
- **CMB-24** The pointer of rows 2 and 3 is stored in `PlayerBoard` when the state is entered.

## SlideBlade

- **CMB-30** `SlideBlade` has an `Attack` slot (`IAdsrAttackModel`, ADSR state Attack, default `SimpleAttack`), a `Decay` slot (`IAdsrModel`, ADSR state Decay, default `LinearSlowdown`), `AttackEndSpeed`, `ColliderRadius` and `ParryFreezeDuration`. The slots accept the same phase models as the ADSR slots of the same name (MOV-43).
- **CMB-31** On entry the job fixes the axis, sets speed to 0 and phase to Attack, and creates an attack: a circle of `ColliderRadius` centred on the player and moving with it, targets `Enemy`, parry kind `Blade`.
- **CMB-32** Each frame, in order:
  1. From the second frame of the state on: if the player's movement cast hit a platform on the previous frame, velocity becomes its projection on that platform's tangent and the job ends. Only a hit counts: the attack collider touching a platform, or the player passing through a one-way platform, does not end the job.
  2. Attack phase: `speed = Attack.Calculate(speed, 1, dt, AttackEndSpeed)`; if `Attack.IsFinished(speed, AttackEndSpeed)` the phase becomes Decay.
  3. Decay phase: `speed = Decay.Calculate(speed, 1, dt, AttackEndSpeed)`; if `speed <= AttackEndSpeed` the job ends after this frame's movement.
  4. Velocity is `axis * speed`.
- **CMB-33** Gravity does not act during the state. When the job ends by step 3, velocity stays `axis * AttackEndSpeed`, and the next state starts from it.
- **CMB-34** When the attack's `Parried` fires, the attack is destroyed at once, velocity becomes 0 and the player stays still for `ParryFreezeDuration`. Then the job ends.
- **CMB-35** The slots are read on every frame, so a model swapped while the game runs takes effect at once (CFG-67).

## SimpleFire

- **CMB-40** `SimpleFire` has `Distance`, `Width`, `TraceDuration`, `TraceColor`, `ExplosionRadius`, `ExplosionDuration` and `ExplosionColor`.
- **CMB-41** On entry the job creates an attack: a rectangle `Distance` long and `Width` wide, centred at `center + axis * Distance / 2` and rotated to the axis, targets `Enemy`, parry kind `Fire`. The attack takes part in exactly one Resolve, then the job ends and destroys it.
- **CMB-42** Velocity does not change during the state and gravity does not act.
- **CMB-43** When the attack's `Parried` fires:
  1. `t = clamp(min over ParriedBoxes of dot(box position - center, axis) / 2, 0, Distance)`. The parry point is `center + axis * t`, so it lies on the axis.
  2. The attack's `ParryPoint` is set to it.
  3. The attack becomes a rectangle `t` long and `Width` wide from the center to the parry point. It still hits in pass 2.
  4. An explosion starts at the parry point (CMB-45).
- **CMB-44** A trace line of `Width` goes from the center to the far end of the attack and fades out over `TraceDuration`.
- **CMB-45** The explosion is an attack: a circle of `ExplosionRadius`, targets `Player` and `Enemy`, parry kind `None`. Its owner job lives as long as the game loop, not the fire state job. It takes part in exactly one Resolve. A filled circle of `ExplosionColor` fades out over `ExplosionDuration`.

## SimpleDownAttack and BounceState

- **CMB-50** `SimpleDownAttack` has `FallSpeed` and `ColliderRadius`.
- **CMB-51** On entry the job creates an attack: a circle of `ColliderRadius` centred on the player and moving with it, targets `Enemy`, parry kind `Blade`. Each frame velocity is `(0, -FallSpeed)`.
- **CMB-52** The job ends when the player is on ground.
- **CMB-53** When the attack's `Parried` fires, the attack is destroyed at once and the job ends as ended by a parry (CMB-22 row 5).
- **CMB-54** `BounceState` has `VerticalStartSpeed`, `VerticalFriction` and a `HorizontalMovement` slot (MOV-02). On entry velocity is `(0, VerticalStartSpeed)`. Each frame `vx` comes from the movement model and `vy` decreases by `VerticalFriction * dt`. It is not a `JumpState`.

## Hits and visuals

- **CMB-60** There is no health. When a hurtbox's `Hit` fires, its entity blinks `HitBlinkColor` for `HitDuration`. The player has these fields in `PlayerSpecs`, the enemy in `EnemySpecs`.
- **CMB-61** The player is drawn as a filled circle of `ColliderRadius` in `Color`. `PlayerSpecs` has no `Width` or `Height`.
- **CMB-62** The player hurtbox is a circle of `ColliderRadius`, side `Player`. It follows a change of `ColliderRadius` while the game runs.
- **CMB-63** Traces, blinks and fades are visuals that do not affect Resolve.

## Enemy

- **CMB-70** The enemy stands at a fixed point of the scene and never moves. Nothing collides with it.
- **CMB-71** `EnemySpecs` is a model in the `GameConfig.Enemy` slot. It replaces `BossSpecs`.

  | Group | Fields |
  |---|---|
  | Body | `Color`, `Width`, `Height`, `HitDuration`, `HitBlinkColor`, `AttackGap` |
  | Blade | `BladeTelegraphDuration`, `BladeTelegraphColor`, `BladeLength`, `BladeWidth`, `BladeAttackDuration`, `BladeAttackColor`, `BladeParriedDuration`, `BladeParriedColor` |
  | Fire | `FireTelegraphDuration`, `FireAimingDuration`, `FireTelegraphColor`, `FireDistance`, `FireWidth`, `FireTraceDuration`, `FireTraceColor`, `FireParriedDuration`, `FireParriedColor` |

- **CMB-72** The enemy is drawn as a filled rectangle `Width × Height` in `Color`. Its hurtbox has the same rectangle, side `Enemy`.
- **CMB-73** The enemy job loops: blade attack, `AttackGap`, fire attack, `AttackGap`.
- **CMB-74** Blade attack. The aim is `normalize(player - enemy)` when the attack starts. The blade rectangle is `BladeLength × BladeWidth`, centred at `enemy + aim * BladeLength / 2` and rotated to the aim.
  1. Telegraph: a parrybox with the blade rectangle, `Accepts` `Blade`, side `Enemy`, and the enemy coloured `BladeTelegraphColor`. It lasts `BladeTelegraphDuration` or until the parrybox is parried, whichever comes first. The parrybox is destroyed at the end.
  2. Not parried: an attack with the blade rectangle, targets `Player`, parry kind `None`, for `BladeAttackDuration`, drawn in `BladeAttackColor`.
  3. Parried: no attack. The enemy is coloured `BladeParriedColor` for `BladeParriedDuration`.
- **CMB-75** Fire attack. During the first `FireAimingDuration` of the telegraph the aim follows `normalize(player - enemy)`; then it is fixed.
  1. Telegraph: a parrybox with the hurtbox rectangle, `Accepts` `Fire`, side `Enemy`, and the enemy coloured `FireTelegraphColor`. It lasts `FireTelegraphDuration` or until the parrybox is parried. The parrybox is destroyed at the end.
  2. Not parried: an attack, a rectangle `FireDistance × FireWidth` centred at `enemy + aim * FireDistance / 2`, rotated to the aim, targets `Player`, parry kind `None`. It takes part in exactly one Resolve. A trace of `FireTraceColor` fades out over `FireTraceDuration`.
  3. Parried: a trace from the enemy to `ParriedBy.ParryPoint`, or to `(enemy + player) / 2` if it is empty, fades out over `FireTraceDuration`. The enemy is coloured `FireParriedColor` for `FireParriedDuration`.
- **CMB-76** A hit blink (CMB-60) shows over the telegraph and parried colours.

## Verification

- CMB-32 steps 2 to 4, CMB-35 and CMB-43 step 1: automated tests with xUnit v3 on `SlideBlade.Step` and `CombatMath.FireParryDistance`, with a fixed `dt`.
- CMB-11 to CMB-18: manual scenarios in `Test.CombatResolve` (`SolsDawn.Gameplay/Test.cs`) that build attacks, parryboxes and hurtboxes and log `ok` or `failed` for each rule.
- CMB-01 to CMB-07, CMB-10, CMB-20 to CMB-24, CMB-30, CMB-31, CMB-32 step 1, CMB-33, CMB-34, CMB-40 to CMB-42, CMB-43 steps 2 to 4, CMB-44, CMB-45, CMB-50 to CMB-54, CMB-60 to CMB-63 and CMB-70 to CMB-76: manual check in the running game.

## Known gaps

- A fast attack can pass over a thin parrybox between two frames. At 60 fps a 1-unit parrybox is skipped only above about 60 units/s.
- Input buffering and attack cooldowns are in the backlog.
- Visual lifetimes (traces, fades) do not follow job ownership; this is in the backlog.
