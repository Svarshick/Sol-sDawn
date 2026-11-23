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
| Container | A group of fields inside a model, with no identity of its own, e.g. `Player.gear`. |
| Category | A constraint on which model types fit a slot, e.g. "one-handed". |
| Candidate | A model type that fits a slot (CFG-10). |
| Breadth | How many categories a model type declares. A model attribute defines it; one that does not has breadth 1. |
| `[Default]` | An attribute on a model type that makes it preferred as the default of the slots it fits (CFG-25). |
| Default model | The one built-in model of a model type, like a default for a struct. |
| Root model | The model that is the config itself. |
| Orphan | A model that no model has a slot referencing. |

## Models

- **CFG-01** The configuration consists of only models, constants and containers. Every configuration field is a constant, a slot or a container.
- **CFG-02** Every model has its own GUID. A slot stores the GUID of the model it references.
- **CFG-03** Models nest: a slot's model may have slots of its own.
- **CFG-04** A container holds constants, slots and other containers. It has no GUID, is saved inside the model that owns it, and cannot be searched for or referenced on its own.
- **CFG-05** A container may hold runtime state. Runtime state is not saved.
- **CFG-06** A slot is never empty. A new slot references the default model of the type chosen by CFG-25.

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
- **CFG-25** A slot's default type is chosen among its candidates. Each rule applies to the candidates the previous rule left:
  1. Candidates marked `[Default]`, if there are any.
  2. The candidates with the smallest breadth.
  3. The first by type name in ordinal order.

  Example: `M1` is marked and fits "a" and "b", `M2` is marked and fits "b" and "c". A slot of "a" gets `M1`, a slot of "c" gets `M2`, and a slot of "b" gets `M1` by rule 3.
- **CFG-26** When rule 3 of CFG-25 decides between more than one candidate, a warning names the slot and the tied types. A slot with no candidate is an error.
- **CFG-27** Defaults are checked for every slot of every model type, including slots inside containers, when the Library is initialized, and for the root type when it is loaded. All warnings are logged in one message, and all errors are raised in one exception.

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

- **CFG-66** A model type may implement `IConstrainedModel` to keep its fields consistent. `Constrain(editedField)` runs after an inspector edit of a field of that model, with the field's name, and after the model is loaded from a file, with `null`. It may change any field of the same model. A change it makes on load is not an unsaved change.
- **CFG-67** Edits apply immediately while the game runs. Values read once at creation, such as the player's collider radius, are rebuilt when their model changes.
- **CFG-68** A field missing from a model file takes the code's default value.
- **CFG-69** A model file whose model type no longer exists is left untouched on disk and logged.

## Storage

- **CFG-90** The Library knows no path. `SolsDawn.Gameplay` passes one to `Library.Initialize(path)` through `GameplayAPI.InitializeLibrary` (API-10).
- **CFG-91** Model files live in `SolsDawn.Gameplay/_Data/`, shared by all platforms. The folder is not ignored by git, so designers can commit their data. Developers' working data there is not committed.
- **CFG-92** Debug builds read and write that source folder directly. Release builds read a copy placed beside the executable at build time.
- **CFG-93** The Library is a store of the save system (SAV). The in-memory value changes every frame during an edit, and files are written only on save (SAV-01), only for models with unsaved changes.
- **CFG-94** Writes go to a temporary file that then replaces the real one, so a crash cannot corrupt a model (SAV-04).
- **CFG-95** Every change to a model is an unsaved change of that model: an edit, a rename, a new copy, a slot change, and the most recent use of CFG-43. Deleting a model removes its file on save. Two exceptions change files at once or not at all:
  - The root model created on first launch (CFG-18) is written at once.
  - Removing a missing GUID from history (CFG-51) changes memory only and is not an unsaved change.

## UI

- **CFG-70** Constants are edited through input fields. A slider may be added when the field has a min and max.
- **CFG-71** A slot is one row of two zones. The left zone shows a fold arrow and `<SlotName>`. The right zone shows `<ModelName>`, styled as a dropdown field. Right zones and the input fields of constants line up in one column across rows, including nested ones.
- **CFG-86** Rows follow the hierarchy: the names of all rows at one depth start at the same x, whether the row is a constant, a slot with or without a fold arrow, or a nested container. Each depth is indented by one arrow width. The fold arrow is drawn in the margin left of the name and never shifts it. A nested container folds like a slot, unfolded by default, and has no right zone.
- **CFG-87** An unfolded slot or container draws a faint vertical guide line under its arrow, down to its last child row.
- **CFG-72** Clicking the right zone opens the slot's search.
- **CFG-73** The search opens as an overlay on top of other elements, as in Chrome or Unity.
- **CFG-74** Double-clicking the right zone edits the model's name.
- **CFG-75** Clicking anywhere in the left zone folds or unfolds the nested model's fields. Slots are folded by default, and the fold state is kept when the slot's model changes. A slot whose model has no fields to show has no fold arrow, and clicking its left zone does nothing. A slot in the error state of CFG-52 always folds, so its error stays reachable.
- **CFG-76** Default models are always at the bottom of the results, in muted gray, in the format `default: <ModelType>`.
- **CFG-77** A row that will be copied is slightly highlighted. A row that will be referenced has the regular color, except default rows (CFG-76).
- **CFG-78** Colors describe what the click will do, not whether the model is an orphan, so they change while Shift is held.
- **CFG-79** The far right of each non-default row has a red X that deletes the model.
- **CFG-80** Palette (not final): minimalist, calm colors. Base is blue-gray, deletion is burgundy or muted red, copy is muted green.
- **CFG-81** The Library Browser is a debug tool. It is closed by default and opened manually.
- **CFG-82** Double-clicking the left zone of a slot that folds focuses it: the inspector then shows only that slot's row, unfolded, with its fields below. Other slots, including the root's other slots, are hidden. Double-clicking a slot inside the focused one focuses that slot instead.
- **CFG-83** The focused row's left zone shows the path of slot and container names from the inspected model, joined by `/`. A path of more than three names keeps the first and the last two, with `...` between them, e.g. `Player/.../Movement/Attack`. Hovering it shows the full path.
- **CFG-84** Double-clicking the focused row's left zone shows the whole model again. Focus follows the path, not the models: it survives a slot on the path changing its model, and ends by itself when the path no longer exists.
- **CFG-85** Focus is kept per place a model is inspected, so the main inspector and the Library Browser focus independently, even on the same model.

## Verification

- CFG-01 to CFG-69 and CFG-90 to CFG-95: automated tests with xUnit v3, headless, against a temporary folder, in a new test project. Focus behaviour in CFG-23 and CFG-62 and the build-time copy in CFG-92 are checked manually.
- CFG-70 to CFG-87: manual checklist in the running game.

## Known limits

None known.

## Known gaps

None known.
