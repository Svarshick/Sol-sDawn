# SolsDawn

MonoGame game on .NET 10.

## Projects

- `SolsDawn.Kernel`: game objects, pool, time, transforms. A separate assembly so the `internal` members of `GameObject`, `Component` and the pool stay unreachable from other code.
- `SolsDawn.Engine`: developer space. One namespace, `SolsDawn.Engine`; folders only group files:
  - `Host/`: the MonoGame `Game`, rendering, camera.
  - `Api/`: `GameplayAPI`, the single file of designer entry points.
  - `Entities/`: `Entity` and `State` base classes, the player and the enemy body.
  - `Physics/`, `Input/`, `Pipeline/` (Job and Event coroutines), `Animations/`.
  - `Movement/`: movement and phase model contracts. `Combat/`: attacks, hurtboxes, parries and their resolve.
  - `World/`: level geometry such as platforms. `Levels/`: the level runner and `Spawn`.
  - `Config/`: configuration and save systems. `Tools/`: inspector, logging, debug windows, Speed Graph.
- `SolsDawn.Gameplay`: designer space. Startup (`Main`), the root `GameConfig`, levels marked `[Level]`, enemy behaviours and phase model implementations.
  Designer code sees only Engine's public surface, with every entry point in `Api/GameplayAPI.cs`. The analyzer in `SolsDawn.Analyzers` makes any `using` directive, and any name outside the `<Using>` items of its csproj, a compile error (`SolsDawn.Engine/Api/GameplayAPI.spec.md`).
- `SolsDawn.Analyzers`: the Roslyn analyzer that enforces the designer boundary.
- `SolsDawn.Tests`: xUnit v3 tests of Engine and of the designer models in Gameplay.
- `SolsDawn.Content`: MonoGame content (textures, sound, other game files).
- `SolsDawn.DesktopGL`: platform entry point for OpenGL.

## Build

`dotnet` is installed through mise, so prefix commands with `mise exec --`.

- Compile check of all game code: `mise exec -- dotnet build SolsDawn.Gameplay/SolsDawn.Gameplay.csproj`
- Tests: `mise exec -- dotnet run --project SolsDawn.Tests/SolsDawn.Tests.csproj`. `dotnet test` does not work with this xUnit v3 setup.
- Full platform build: `mise exec -- dotnet build SolsDawn.DesktopGL/SolsDawn.DesktopGL.csproj`. Its content step compiles shaders and needs Wine.

## Rules

- Do not write comments in code. If an existing comment could be important, ask before removing it.
- Generated and third-party code keeps its comments: `SolsDawn.Content`, `SolsDawn.DesktopGL`, `DrawVertDeclaration.cs`, `ImGuiRenderer.cs`, `InputExtended.cs`.
- Do not commit `SolsDawn.Gameplay/_Data/`. Designers own what is committed there, and local working data must not overwrite it.
- The local `SolsDawn.Gameplay/_Data/` can be deleted safely, until the user says otherwise.
- `_Data` snapshots live in dead-end commits under `data/<hash>` tags (`data/<hash>-2`, `-3`… for repeat saves), never on a branch. The tagged commit's parent is the code commit the data belongs to, and it adds only `_Data`. Use `scripts/data.sh` as `git data`; building `SolsDawn.Gameplay` registers that alias in the local git config:
  - `git data save`: snapshot the local `_Data` on HEAD, tag and push it. The index and working tree are not touched.
  - `git data list [-v]`: fetch and list snapshots, newest first. `-v` adds the subject of the code commit each snapshot belongs to.
  - `git data load [tag]`: replace the local `_Data` with the newest snapshot, or the given one. The previous local `_Data` moves to `_Data.bak`.
- "Change the config according to `data/<hash>`" means: read `_Data` from that tag (`git show data/<hash>:SolsDawn.Gameplay/_Data/<file>`) and fit the current local `_Data` to it, migrating what the code changed since the tag's parent.
- Everything code-specific is in English: code, specs, identifiers.
- Tests use xUnit v3.

## Specs

- A spec is a `*.spec.md` file next to the code it describes.
- The spec is the source of truth. Where code disagrees, the code is wrong unless the spec is changed first.
- Workflow: change the spec, implement it, check the result against the requirement IDs.
- Each spec has a rule in `.claude/rules/` whose `paths` list the files it covers. When adding a spec or moving those files, update the rule.
- Specs cover developer code only. Designer code in `SolsDawn.Gameplay` has no spec; specs state the contracts it must follow.

## Docs

`Docs/` holds designer drafts and may be in Russian. It is not a source of truth for code behaviour.
