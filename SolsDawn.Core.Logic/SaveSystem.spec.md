# Save System Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `SaveSystem.cs`, the save, title and exit handling in `Game.cs`.

Requirements are `SAV-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Store | Data the save system writes to disk: the Library (CFG) and the graph store (SPG). |
| Unsaved changes | Changes a store holds in memory and has not written to disk yet. |
| Save | Writing the unsaved changes of every store. |

## Saving

- **SAV-01** A store changes only memory. Its files are written only on save.
- **SAV-02** Save writes only what changed: changed and new items are written, and the files of deleted items are removed. Afterwards no store has unsaved changes.
- **SAV-03** Ctrl+S (left or right Ctrl) saves. It works whether or not an ImGui window or text field has the keyboard. A text field's draft that is not applied yet, such as a rename being typed, is not part of the save.
- **SAV-04** Each file is written to a temporary file that then replaces the real one, so a crash cannot corrupt it.
- **SAV-05** If save fails, the error is logged and the changes that were not written stay unsaved.
- **SAV-06** A store that was not initialized has no unsaved changes.

## Indication and exit

- **SAV-10** While any store has unsaved changes, the game window title ends with ` *`.
- **SAV-11** Closing the game with unsaved changes is cancelled, and a modal asks `Unsaved changes. Save?` with Yes, No and Cancel.
  - Yes saves and closes. If save fails (SAV-05), the game stays open.
  - No closes without saving.
  - Cancel closes the modal and the game goes on.
- **SAV-12** Closing the game without unsaved changes closes it at once.

## Verification

- SAV-01, SAV-02, SAV-04 and SAV-06: automated tests of each store with xUnit v3, against a temporary folder.
- SAV-03, SAV-05 and SAV-10 to SAV-12: manual check in the running game.

## Known limits

- A crash loses the unsaved changes.

## Known gaps

None known.
