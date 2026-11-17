# Speed Graph Specification

This spec is the source of truth. Where the code disagrees, the code is wrong unless the spec is changed first.

Code: `SolsDawn.Core.Logic/SpeedGraph/`, the Speed Graph checkbox in `Levels/DebugWindow.cs` and the sample call in the player phase of `Levels/LevelRunner.cs`.

Requirements are `SPG-nn`.

## Terminology

| Term | Meaning |
|---|---|
| Frame | One game update. The game runs at a fixed frame rate, 60 frames per second. |
| Sample | The player's velocity in one frame, X and Y, or a gap when there is no live player. |
| Recording | The live sequence of samples. |
| Graph | A saved sequence of samples with a name. |
| Graph store | The saved graphs. It is a store of the save system (SAV). |
| Frame number | A sample's index. Recording frames count from 0 at game start or the last Clear, and dropping old samples does not renumber them. Graph frames count from 0. Time is `frame / frame rate`. |
| First, last | The frame number of the oldest sample, and one past the newest. For a graph, 0 and its length. |
| Visible range | The frames the graph area shows, `[start, end)`. |
| Period | The length of the visible range, `end - start`, in frames. |
| Shown value | What the graph draws for a sample, chosen by the value mode (SPG-40). |

## Recording

- **SPG-01** Each frame, right after the player phase updates `PlayerController`, the recording takes one sample: `G.Player.Board.Velocity` when `G.Player` is alive, a gap otherwise.
- **SPG-02** The recording keeps the last 5 minutes, 18000 samples. When it is full, each new sample drops the oldest.
- **SPG-03** Recording runs from game start, whether the window is open or not.
- **SPG-04** Stop pauses the recording: frames while it is stopped add no samples and no gaps. Resume continues it.
- **SPG-05** Clear removes every sample. Frame numbers start again from 0.
- **SPG-06** A sample keeps both X and Y. The value mode only changes the drawing.
- **SPG-07** Switching or restarting a level does not touch the recording. Frames without a player are gaps (SPG-01).

## Graphs

- **SPG-10** Save adds a graph with the recording's samples in the visible range, gaps included. Frames of the visible range before the first sample are not part of it. Save is disabled when the visible range holds no sample. The recording goes on.
- **SPG-11** A new graph is named `graph<N>`. N is one more than the largest number among graphs named `graph<number>`, or 1 when there is none.
- **SPG-12** Graph names are unique, ignoring case, and contain no spaces: spaces are replaced with underscores. A rename to an empty or taken name is rejected, and the name stays.
- **SPG-13** Crop replaces a graph's samples with those in the visible range. It is disabled when the visible range covers the whole graph.
- **SPG-14** A graph keeps its id, name, frame rate, creation time, and X and Y of every sample, with gaps.
- **SPG-15** Adding, renaming, cropping and deleting graphs are unsaved changes of the graph store (SAV-01). The recording is never written to disk.

## Storage

- **SPG-20** `SolsDawn.Core.Logic` knows no path. `SolsDawn.Gameplay` passes one to `SpeedGraphs.Initialize(path)`: `Graphs/` inside the `_Data` folder of CFG-91.
- **SPG-21** Each graph is one file, named by its id, in that folder. The Library reads only files directly in `_Data`, so it never sees graph files.
- **SPG-22** A graph file that cannot be read is logged and left untouched on disk. A loaded graph whose name is taken gets `_2`, `_3` and so on appended in memory.

## Window and modes

- **SPG-30** The Speed Graph window is a debug tool. It is closed by default and opened by a checkbox in the debug window, next to Library Browser.
- **SPG-31** The window has three modes: Live shows the recording, View shows one graph, List lists them. Selecting chooses what Live or View shows: the recording or a graph. The recording is selected at start.
- **SPG-32** The toggle button is the first item at the top left of the window in every mode, so switching back never needs a mouse move. In Live and View it reads `List` and opens List. In List it reads `Graph` and shows the selection: Live for the recording, View for a graph.
- **SPG-33** List shows `Live` pinned at the top, then the graphs, newest first, each with its duration in seconds. Clicking a row selects it. Double-clicking a graph row renames it in place. Enter or clicking elsewhere applies the name, and Escape cancels. A red X at the right of each graph row deletes it.
- **SPG-34** When the selected graph is deleted, the recording is selected.
- **SPG-35** Live has Stop or Resume, Clear and Save. View has Crop and the graph's name, and double-clicking the name renames it as in SPG-33.

## Visible range

- **SPG-40** The value modes are Total, Horizontal and Vertical. Total shows `|v|`, Horizontal `v.X` and Vertical `v.Y`. With Absolute checked, Horizontal and Vertical show `|v.X|` and `|v.Y|`. Absolute is disabled in Total. The value mode and Absolute are shared by Live and View.
- **SPG-41** The period is entered in seconds and rounded to whole frames, at least 2. The field shows seconds and frames.
- **SPG-42** In Live the period is at most the recording limit (SPG-02), and it is 10 seconds at start. In View it is at most the graph's length, and opening a graph shows the whole graph.
- **SPG-43** Changing the period in the field keeps the end of the visible range.
- **SPG-44** The end of the visible range is at most last. Its start is before first only when the period is longer than the samples there are, and then the graph area is empty before first. When old samples drop out of the recording, the range moves forward to stay on samples.
- **SPG-45** In Live the visible range follows the recording while its end is last: each new sample moves the range along. Moving the end away from last stops following, and moving it back to last follows again.
- **SPG-46** The mouse wheel over the graph area zooms. Each notch multiplies or divides the period by 1.25 and keeps the frame under the cursor in place. While following, the end stays instead and following goes on.
- **SPG-47** Dragging with the left mouse button in the graph area pans the visible range.
- **SPG-48** The overview strip below the graph area shows the whole recording or graph, scaled to its width, and the visible range as a band.
  - Dragging the band moves the range.
  - Dragging an edge of the band moves only that edge.
  - Pressing outside the band centers the range there, and dragging then moves it.

## Axis

- **SPG-50** The axis max field takes a speed of at least 0, and it is 0 at start. The effective max M is the larger of the field and the highest absolute shown value in the visible range. When M is 0, M is 1.
- **SPG-51** Total and Absolute draw from 0 to M. Signed Horizontal and Vertical draw from -M to M, with a line at 0.

## Drawing

- **SPG-60** Frame f is drawn at the center of its column: `x = left + (f - start + 0.5) * width / period`.
- **SPG-61** Vertical grid lines are drawn at frames that are multiples of the frame step. The frame step is the smallest value of the step list whose lines are at least 6 px apart. The step list is 1, 2, 3, 5, 10, 15 and 30 frames, then 1, 2, 5, 10, 15, 30, 60, 120 and 300 seconds.
  Example: a period of 10 frames on 400 px gets a line at every frame. A period of 600 frames on 500 px gets a line every 10 frames.
- **SPG-62** Major lines are brighter and labelled with their time in seconds. They are at multiples of the major step: the smallest value of the step list that is a multiple of the frame step and whose lines are at least 60 px apart.
- **SPG-63** Horizontal grid lines are at multiples of the smallest speed step of the form 1, 2 or 5 times a power of 10 whose lines are at least 24 px apart. Each is labelled with its speed.
- **SPG-64** Samples are joined by a line, and a gap breaks it. When frames are at least 6 px apart, each sample is also drawn as a dot.
- **SPG-65** Hovering the graph area draws a cursor line at the nearest frame, and a tooltip shows its frame, time, X, Y and `|v|`, or `gap`.

## Verification

- SPG-02, SPG-04 to SPG-06, SPG-10 to SPG-22, SPG-40 to SPG-46, SPG-50 and SPG-61 to SPG-63: automated tests with xUnit v3, headless, against a temporary folder. The gap of SPG-01 is tested too. The UI parts of those are checked manually.
- SPG-01, SPG-03, SPG-07, SPG-30 to SPG-35, SPG-47, SPG-48, SPG-51, SPG-60, SPG-64 and SPG-65: manual check in the running game.

## Known limits

None known.

## Known gaps

None known.
