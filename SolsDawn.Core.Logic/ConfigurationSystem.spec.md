# Configuration System Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `ConfigurationSystem.cs`, `Inspector.cs`.

Requirements are `CFG-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Constant | A plain value field: float, string, Vector2, and so on. |
| Model type | A class of configuration, e.g. `Player` with `speed` and `name`. |
| Model | An instance of a model type, with its own GUID. |
| Slot | A model-typed field, e.g. `Player.weapon`. |
| Container | A group of fields inside a model, with no identity of its own, e.g. `MotionSolver`. |
| Category | A constraint on which model types fit a slot, e.g. "one-handed". |
| Default model | The one built-in model of a model type, like a default for a struct. |
| Root model | The model that is the config itself. |
| Orphan | A model that no model has a slot referencing. |

## Models

- **CFG-01** The configuration consists of only models, constants and containers. Every configuration field is a constant, a slot or a container.
- **CFG-02** Every model has its own GUID. A slot stores the GUID of the model it references.
- **CFG-03** Models nest: a slot's model may have slots of its own.
- **CFG-04** A container holds constants, slots and other containers. It has no GUID, is saved inside the model that owns it, and cannot be searched for or referenced on its own.
- **CFG-05** A container may hold runtime state. Runtime state is not saved.
- **CFG-06** A slot is never empty. A new slot references the default model of the type named by its category.

## Categories

- **CFG-10** A slot accepts a model only if the model's type fits the slot's type and has the slot's required category.
  Example: `Player.weapon` requires "one-handed". `Sword` is "one-handed, melee" and fits. `Spear` is "two-handed, melee" and does not.

## Root model

- **CFG-15** The root model is an ordinary model file with the reserved GUID `root`, stored as `root.json` beside the other models. It persists across launches.
- **CFG-16** The root model is never an orphan.
- **CFG-17** The root model's type, `GameConfig`, is defined in `SolsDawn.Gameplay` and is not offered in any search.
- **CFG-18** `Library.LoadRoot<GameConfig>()` loads the root model, or creates it on first launch.
- **CFG-19** Runtime data such as `PlayerBoard` is not part of the config. It reads its models from `GameConfig`.

## Default models

- **CFG-20** There is exactly one default model for each model type.
- **CFG-21** Default models are read-only. They are never changed, cannot be deleted and create no file.
- **CFG-22** Selecting a default model in search references it. Shift has no effect on this, and there is no way to copy a default model explicitly.
- **CFG-23** The first edit of a default model through a slot creates a copy, and the slot references that copy. Changing a slot inside a default model counts as an edit. When defaults are nested, every default between the edited one and its nearest non-default owner is copied. Focus does not move.
- **CFG-24** A copy made by CFG-23 is named `<OwnerName>_<SlotName>`.

## Search

- **CFG-30** Each slot has a search: an input field and a dropdown list of suitable options.
- **CFG-31** Search returns only models that fit the slot (CFG-10).
- **CFG-32** Clicking a result makes the slot either reference the selected model, or reference a new copy of it.
- **CFG-33** Default action by model kind:

  | Model | Action |
  |---|---|
  | Default model | Reference, always |
  | Orphan | Reference |
  | Not an orphan | Copy |

- **CFG-34** Holding Shift swaps the action for orphans and non-orphans: orphans are copied, non-orphans are referenced.
- **CFG-35** Search does not offer a model if selecting it would make a model contain itself.

## Search tags

- **CFG-40** `has:<ModelName>` returns models used by `<ModelName>` or by models it references, transitively (literally `modelName/*`).
- **CFG-41** `had:<ModelName>` is the same as `has:`, over all time (historical references).
- **CFG-42** `default:<ModelType>` returns only default models.
- **CFG-43** Among results with the same name, the most recently used model (edited, selected, etc.) comes first.
- **CFG-44** `has:` and `had:` match by substring against all models with a matching name. Each result is listed once.

## Deletion and history

- **CFG-50** Deleting a model does not immediately purge its GUID from the history of other models.
- **CFG-51** When `had:` meets a GUID that no longer exists, it removes that GUID from the history and does not display it.
- **CFG-52** Deleting a model that a slot still references produces an error state in that slot. It is visible in the UI and the program does not crash.
- **CFG-53** A slot in that error state uses a phantom default model until the user explicitly selects an existing model.
- **CFG-54** A phantom is read-only. Editing it does not create a copy.

## Copying and naming

- **CFG-60** A copy is a new model with a new GUID.
- **CFG-61** Copying a model also copies its nested non-default models, recursively, so the copy is independent. Nested default models stay referenced.
- **CFG-62** After an explicit copy from search, focus moves to the name field with the name selected. Typing replaces the name and Enter keeps it.
- **CFG-65** Model names contain no spaces. Spaces are replaced with underscores, because search tags are space-separated.

## Runtime and file format

- **CFG-67** Edits apply immediately while the game runs. Values read once at creation, such as the player's collider radius, are rebuilt when their model changes.
- **CFG-68** A field missing from a model file takes the code's default value.
- **CFG-69** A model file whose model type no longer exists is left untouched on disk and logged.

## Storage

- **CFG-90** `SolsDawn.Core.Logic` knows no path. `SolsDawn.Gameplay` passes one to `Library.Initialize(path)`.
- **CFG-91** Model files live in `SolsDawn.Gameplay/_Data/`, tracked by git and shared by all platforms.
- **CFG-92** Debug builds read and write that source folder directly. Release builds read a copy placed beside the executable at build time.
- **CFG-93** The in-memory value changes every frame during an edit. The file is written once when the edit ends, and only for the edited model.
- **CFG-94** Writes go to a temporary file that then replaces the real one, so a crash cannot corrupt a model.

## UI

- **CFG-70** Constants are edited through input fields. A slider may be added when the field has a min and max.
- **CFG-71** A slot displays `<SlotName> <ModelName>`.
- **CFG-72** Clicking a slot opens its search.
- **CFG-73** The search opens as an overlay on top of other elements, as in Chrome or Unity.
- **CFG-74** Double-clicking a slot edits the model's name.
- **CFG-75** An arrow on the left of a slot expands the nested model's fields.
- **CFG-76** Default models are always at the bottom of the results, in muted gray, in the format `default: <ModelType>`.
- **CFG-77** A row that will be copied is slightly highlighted. A row that will be referenced has the regular color, except default rows (CFG-76).
- **CFG-78** Colors describe what the click will do, not whether the model is an orphan, so they change while Shift is held.
- **CFG-79** The far right of each non-default row has a red X that deletes the model.
- **CFG-80** Palette (not final): minimalist, calm colors. Base is blue-gray, deletion is burgundy or muted red, copy is muted green.
- **CFG-81** The Library Browser is a debug tool. It is closed by default and opened manually.

## Verification

- CFG-01 to CFG-69 and CFG-90 to CFG-94: automated tests with xUnit v3, headless, against a temporary folder, in a new test project. Focus behaviour in CFG-23 and CFG-62 and the build-time copy in CFG-92 are checked manually.
- CFG-70 to CFG-81: manual checklist in the running game.

## Known limits

Accepted for now, with the condition that ends them.

- **LIM-01** A container's runtime state, such as `MotionSolver.Phase`, is one value per model, so entities referencing the same model overwrite each other's state. This is safe only while one entity uses these models. It must be changed before a second entity uses the same config.

## Known gaps

Current code against this spec. Remove each row when fixed.

| Requirement | Current code |
|---|---|
| CFG-05 | `MotionSolver.Phase` is written into model files. |
| CFG-06 | An unset slot is filled by creating a full copy of the default, with files. |
| CFG-15 | No root model. `new PlayerBoard()` is never saved, so the first inspector draw creates a new model tree on every launch. |
| CFG-21, CFG-54 | The inspector still edits fields of the shared default instance when showing a default or phantom. |
| CFG-22, CFG-23, CFG-24, CFG-33 | Selecting a default always copies it. Nothing happens on first edit. |
| CFG-35 | No check. A cycle would make copying recurse forever. |
| CFG-44 | A model reached through several matching parents is listed once per parent. |
| CFG-50 | `Library.Initialize` and the inspector's delete handler prune history eagerly. |
| CFG-61 | Copying copies every nested model, including defaults. |
| CFG-62 | Focus does not move after a copy. |
| CFG-67 | The player's collider is built once from the default model. |
| CFG-72, CFG-74, CFG-75 | Click expands the editor, double-click opens search, and the name is edited inside the editor. |
| CFG-76 | Default rows are labelled `<Name> [Default]`. |
| CFG-80 | Copy color is teal. |
| CFG-81 | The Library Browser opens by default (`_showLibraryBrowser = true` in `SolsDawn.Gameplay/Debug.cs`). |
| CFG-90 to CFG-94 | `Library` defaults to `bin/.../Presets`, writes on every edited frame for the model and its parents, and writes files in place. |
