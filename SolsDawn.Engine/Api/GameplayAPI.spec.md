# Gameplay API Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `SolsDawn.Engine/Api/GameplayAPI.cs`, the access modifiers of `SolsDawn.Engine`, `SolsDawn.Analyzers/`, and the `<Using>` items of `SolsDawn.Gameplay/SolsDawn.Gameplay.csproj`.

Requirements are `API-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Designer code | Every source file of `SolsDawn.Gameplay`. |
| Designer API | What designer code may name: the public surface of `SolsDawn.Engine` and the imports of API-20. |
| Entry point | A static method, property or constructor call that designer code starts from. |

## Surface

- **API-01** The public types and members of `SolsDawn.Engine` are exactly the designer API. Everything else in Engine is `internal`.
- **API-02** `SolsDawn.Tests` and the platform assemblies see Engine internals through `InternalsVisibleTo`. `SolsDawn.DesktopGL` builds the assembly `SolsDawn`.
- **API-03** Every entry point of the designer API is in `GameplayAPI.cs`. Public types elsewhere in Engine are reached through it, or are types designer code implements, derives from or uses as attributes: model interfaces, model and category attributes, inspector attributes, `[Level]`, `CategorySlot<T>` and the specs records.
- **API-04** Constructors of public Engine types are internal. Designer code creates combat objects with `CreateAttack` and `CreateParrybox`, entities with `Spawn`, animations with `Animations`, events with `Event` and `Timer`. Exceptions, because the language or reflection needs them public: `CategorySlot<T>`, specs and board records, primary constructors of animation sets, awaiters and `JobAsyncMethodBuilder.Create`.
- **API-05** Members of public types that only Engine uses are internal, unless the inspector or the save system reads them by reflection: those stay public, and so do the types they need.

## Functions

- **API-10** Startup: `DataDirectory(subdirectory)` is `_Data/subdirectory`, with `_Data` beside the calling source file in Debug builds and beside the executable in Release builds (CFG-91, CFG-92). `InitializeLibrary`, `LoadRoot<T>`, `InitializeSpeedGraphs` and `RunLevels` forward to `Library.Initialize`, `Library.LoadRoot<T>`, `SpeedGraphs.Initialize` and `LevelRunner.Run`.
- **API-11** Math works on `float` and covers what designer code needs instead of `System.Math`: `PI`, `Abs`, `Sign`, `Min`, `Max`, `Clamp`, `Lerp`, `Sqrt`, `Pow`, `Exp`, `Floor`, `Round`, `Sin`, `Cos`, `Tan`, `ASin`, `ACos`, `ATan2`, `Angle`, `Rotated` and `Direction`.
- **API-12** `Direction(from, to)` is `normalize(to - from)`, or `UnitX` when the points are equal.
- **API-13** `Spawn` and `G` are nested in `GameplayAPI` (LVL-08 to LVL-11). `G.Player` is set only by Engine.
- **API-14** `Shapes.EnemyBody(specs)` is the enemy body shape for those specs.

## Boundary

- **API-20** The `<Using>` items of `SolsDawn.Gameplay.csproj` are the only imports of designer code. A namespace item allows the types declared directly in that namespace. An alias or static item allows that type and its nested types.
- **API-21** The analyzer in `SolsDawn.Analyzers` reports error `SD0001` on every `using` directive in designer code, `global using` included. Generated files are not checked.
- **API-22** The analyzer reports error `SD0002` on every name in designer code that binds to a type or member outside the designer API. Allowed: types of `SolsDawn.Gameplay`, the imports of API-20, C# built-in types, tuples, arrays of allowed types, and members whose containing type is allowed. An extension method counts as a member of its declaring class.
- **API-23** `SD0002` is reported once for a member access on a forbidden type, at the type.
- **API-24** Operators, implicit conversions and calls the compiler generates, such as `GetAwaiter`, are not checked.

## Verification

- API-01, API-04: the Gameplay build fails when designer code uses an internal member.
- API-21 to API-24: manual check with a probe file in `SolsDawn.Gameplay` that uses `using`, `System.Math`, `System.IO.Path`, `List<T>` and allowed names; remove the probe after the check.
- API-02, API-03, API-05, API-10 to API-14, API-20: review.
