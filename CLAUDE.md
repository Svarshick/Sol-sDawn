# SolsDawn

MonoGame game on .NET 10.

## Projects

- `SolsDawn.Core`: game objects, pool, time, transforms. Kept apart from `Core.Logic` to isolate protected methods of GameObject and the pool. The split may be changed if there is a good reason.
- `SolsDawn.Core.Logic`: game initialization, graphics details, main systems.
- `SolsDawn.Gameplay`: designer space for configs and mechanics.
- `SolsDawn.Content`: MonoGame content (textures, sound, other game files).
- `SolsDawn.DesktopGL`, `SolsDawn.DesktopVK`: platform entry points for OpenGL and Vulkan.

## Build

`dotnet` is installed through mise, so prefix commands with `mise exec --`.

- Compile check of all game code: `mise exec -- dotnet build SolsDawn.Gameplay/SolsDawn.Gameplay.csproj`
- Tests: `mise exec -- dotnet run --project SolsDawn.Core.Logic.Tests/SolsDawn.Core.Logic.Tests.csproj`. `dotnet test` does not work with this xUnit v3 setup.
- Full platform build: `mise exec -- dotnet build SolsDawn.DesktopGL/SolsDawn.DesktopGL.csproj`. Its content step compiles shaders and needs Wine.

## Rules

- Do not write comments in code. If an existing comment could be important, ask before removing it.
- Generated and third-party code keeps its comments: `SolsDawn.Content`, `SolsDawn.DesktopGL`, `SolsDawn.DesktopVK`, `DrawVertDeclaration.cs`, `ImGuiRenderer.cs`, `InputExtended.cs`.
- `SolsDawn.Gameplay/Test.cs` holds manual scenarios that run in the game loop. They are not unit tests; do not convert them.
- Do not commit `SolsDawn.Gameplay/_Data/`. Designers own what is committed there, and local working data must not overwrite it.
- Everything code-specific is in English: code, specs, identifiers.
- Tests use xUnit v3.

## Specs

- A spec is a `*.spec.md` file next to the code it describes.
- The spec is the source of truth. Where code disagrees, the code is wrong unless the spec is changed first.
- Workflow: change the spec, implement it, check the result against the requirement IDs.
- Each spec has a rule in `.claude/rules/` whose `paths` list the files it covers. When adding a spec or moving those files, update the rule.

Before a second entity uses the same config models as the player, resolve LIM-01 in `SolsDawn.Core.Logic/ConfigurationSystem.spec.md`.

## Docs

`Docs/` holds designer drafts and may be in Russian. It is not a source of truth for code behaviour.
