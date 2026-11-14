# Combat plan (handoff)

Working notes for the fight system: blade, fire, down attack, blade parry, fire parry.
Not a spec. The next step is to turn it into `Combat.spec.md` + a rule in `.claude/rules/`, then implement.

## Feature requirements (from the developer)

Player attacks:
1. **Blade.** Left click on a point. Behaviour depends on the blade model; for now one model: the attack goes in the click direction. A blade collider spawns and moves in that direction, driven by a movement/motion model.
2. **Fire.** A long line. The model sets the distance. Instantaneous.
3. **Down attack.** In the air, Space (jump on the ground) starts a vertical downward attack. Horizontal speed becomes 0.

Mechanics:
1. **Blade parry.** An enemy attack has a parrybox and an actual attack collider. If the player blade hits the blade parrybox, a parry happens.
2. **Fire parry.** Enemy fire is instantaneous, so its parrybox always opens before the fire. Example: the enemy fire parrybox is the same shape as the enemy hitbox. If player fire hits it: enemy and player both fire to the midpoint between them and an explosion happens there.

Enemy: not physical (no movement collision). For the demo it stays at one point and cycles blade, fire, blade, ...

## Already done

- `fc14a1e`: Aether `World` and `Body` removed. Aether is used only as a geometry library.
  - `Collider(GameObject, Shape, Category layer)`: public `Shape`, one `Layer`, `Pose` from `Transform.WorldPosition/WorldRotation`. Registers in `Physics` on creation, unregisters in `OnDestroyImmediate`.
  - `Physics.CircleCast(from, to, radius, layers, filter)`; the result has `Collider`.
  - Layers: `GameplayAPI.Layer` (`Wall`, `Player`, `Enemy`, `None`) over Aether `Category`.
  - Global aliases `Collider`, `Physics` in `SolsDawn.Gameplay.csproj`. Importing the `SolsDawn.Core.Logic.Gameplay` namespace makes `Input` ambiguous.
  - `ColliderAnimation` always uses `Debug.Collider.Category.Default`. Attack and parry colliders need a constructor parameter for the category (`Debug.Collider` already has Attack and Parry colors and minimal times).
- Deferred state-job start (check `git status`; it may not be committed): `Entity.Enter` sets `JobAsyncMethodBuilder.DeferStart`, so the state job's first code runs on its first `Update`, not inside `Enter`. `Entity.UpdateState()` is the only runner of a state job; `Job.Update` is `internal`.
- Backlog in `Docs/todo`: snapshot blocker (async state machines can't be serialized), job visibility (automatic names, `Job.Tag`, ImGui job tree).

## Architecture decisions

### Collision: queries in one resolve phase, no contact events
- Combat never uses callbacks. `Combat.Resolve()` tests shapes with `Collision.TestOverlap(shapeA, 0, shapeB, 0, ref xfA, ref xfB)` on `Collider.Pose`.
- `Resolve` runs inside a top-level job in `GameLoop`, after the player and enemy jobs and before the camera (for example at the start of the `AfterUpdate` loop). Attack jobs can't kill it. All `Event.Fire` calls happen inside the job pipeline, so they may kill jobs and destroy colliders freely.
- **Two passes**: first every attack against parryboxes, then every attack against hurtboxes. A parry anywhere this frame beats a hit this frame.
  - Pass 1: the first live `Parrybox` with the opposite side, `Accepts == attack.Kind`, overlapping → parry.
  - Pass 2: each live hurtbox with the opposite side, not in `attack.AlreadyHit`, overlapping → hit.
- **Lazy deletion instead of copying lists**: skip `IsDestroyed` entries during loops and compact after `Resolve`. New entries go to a to-add list merged at the same point (the same pattern as `Job._subjobs` / `_subjobsToAdd`). The same applies to the `Physics` collider list.
- Overlap is checked every frame (level-triggered), with an `AlreadyHit` set per attack. That catches a parrybox that opens on top of a blade, and makes multi-hit an explicit design choice.
- Tunneling: at 60 fps a 1-unit parrybox is skipped only above ~60 units/s. If needed, test the convex hull of the blade at its previous and current pose (a 4-vertex rectangle gives 8 points, which is the Aether limit).

### Jobs orchestrate combat
- Each attack, parrybox and visual is created inside a job, so killing the job (for example on parry) destroys it.
- The `Docs/behave.attacks.code` "Enemy Pseudocode" maps to `Race`: `r1(warningEndT, pb.End)` → `Race(Timer(warning), pb.Parried)`. That file also has legacy parts (intentions, physics) to rethink later.
- `Event<T>` doesn't exist yet: store the payload (target, parry point) as fields on the attack or parrybox object and fire a plain `Event`.

## Pieces to build

Components, each with a `Collider` and debug draw, created inside a job:
- `Hurtbox`: an entity's damageable area. Has `Side`.
- `Parrybox`: `Side`, `Accepts` (Blade or Fire), `Parried` event, plus who parried it.
- `Attack`: `Side`, `Kind` (Blade, Fire, Down), `Hit` and `Parried` events, `AlreadyHit` set.
- `Combat`: registry of the three lists and `Resolve()`.

Player (`SolsDawn.Gameplay/Entities`):
- Add `AllowedState` flags and `PlayerSpecs` slots for Blade and DownAttack.
- State instances are shared config models (see `Movement.spec.md` MOV-05), so runtime parameters go in `PlayerBoard` (for example the blade target).
- Blade: on the press frame `Camera.ScreenToWorld(Input.Blade.ScreenPosition)` gives the target. The job moves an `Attack` with a movement model slot (`IMovementModel.Calculate`) and finishes at the distance or on `Parried`.
- `PlayerController`: a finished `StateJob` (`IsEnded`) means "pick the next state".
- DownAttack: from `FallState`/`JumpState` on `Input.Jump` (unused in the air today). `vx = 0`, fixed downward `vy`, the `Attack` follows the player's feet, landing exits through the existing `OnGround` branch.
- Fire: a one-frame `Attack`: a rectangle of the model's distance, centred at `origin + dir * dist / 2`, rotated to the direction. It goes through `Resolve` like everything else.
- Input: `Input.Blade` (left click) and `Input.Fire` (right click) give `(ScreenPosition, IsPressed)` on the press frame only. `Input.Jump` is Space pressed.

Enemy (non-physical, has a `Hurtbox`):

```csharp
async Job BladeAttack(Enemy e, BladeSpec s)
{
    var pb = new Parrybox(CreateObject(e.Transform.Position), s.ParryShape, Kind.Blade, Side.Enemy);
    var r = Race(Timer(s.Warning), pb.Parried);
    await r.Finished;
    pb.Destroy();
    if (r.Winner == pb.Parried) { await Parried(e, s); return; }
    new Attack(CreateObject(e.Transform.Position), s.AttackShape, Kind.Blade, Side.Enemy);
    await Timer(s.AttackDuration);
}
```

- Enemy fire: the same pattern. A fire parrybox with the hitbox shape during the telegraph. On `Parried`, the enemy job draws both lines to `(enemy + player) / 2` and creates the explosion there ("enemy side routine").
- Demo loop: `while (true) { await BladeAttack(); await Timer(gap); await FireAttack(); await Timer(gap); }`
- `BossBoard` / `BossSpecs` in `SolsDawn.Gameplay/Entities/BossBoard.cs` hold the old blade, fire and parry tuning fields; reuse or replace them.

## Open design questions (decide in the spec)

1. Does gravity apply during Blade in the air? Does Blade cancel jump momentum?
2. Same-frame parry against a hit on the player: player-favoring (current plan) or strict?
3. Does a blade hit once per target, or once per frame while overlapping?
4. Does fire stop at the first target or pass through to its full distance?
5. Who does the fire-parry explosion damage?

## Next steps

1. Write `Combat.spec.md` next to the combat code, with requirement IDs, and add a rule in `.claude/rules/` with its `paths`.
2. Add a debug category parameter to `Collider`/`ColliderAnimation`, and lazy deletion in `Physics`.
3. Implement `Combat` (components + `Resolve`), then the player states, then the demo enemy.
4. Check the result against the requirement IDs. Manual scenarios go in `SolsDawn.Gameplay/Test.cs`; unit tests in `SolsDawn.Core.Logic.Tests` (xUnit v3, run with `dotnet run`).
