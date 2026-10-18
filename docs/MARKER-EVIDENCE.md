# Timeline markers (0.0.12)

## Reference observation

Target: installed Camtasia 26.2.2.19936 editor on Windows, 2026-10-18. It had an untitled 320×180 project with one 4-second clip. Every reference change made during the observation was undone afterwards, and the marker tray was hidden again.

**Menus and shortcuts**

- Modify > Markers > **Add Timeline Marker** (`Shift+M`) and **Remove All Markers**. Remove All Markers is disabled when there are no markers.
- View > **Show Markers**.
- Edit > Jump To > **Next Marker** (`Ctrl+]`) and **Previous Marker** (`Ctrl+[`).
- In the same Jump To menu: Beginning `Ctrl+Home`, End `Ctrl+End`. Edit > Deselect All is `Ctrl+Shift+D`.
- The timeline tray button's tooltip reads: "Show or hide the Quiz (Ctrl+Q), Marker (Ctrl+M), or Transcript (Ctrl+T) tray above the timeline."

**Marker tray**

- `Ctrl+M` shows a thin **Marker** row between the ruler and the tracks. Its header reads "Marker".
- Add Timeline Marker puts a blue chip labeled **Marker 1** at the playhead. A second marker became **Marker 2**.
- Clicking an empty spot in the tray adds a marker at that time.
- A marker placed after the last clip extended the duration from 00:04 to 00:08.
- Each add is one undo step.

**Marker properties**

- Selecting a marker opens a Properties panel titled **Marker**.
- It shows the marker name, a **Thumbnail** of the frame, and an editable **Marker name** field.
- Right-clicking a chip offers **Rename** and **Delete**.

**First-marker dialog**

When the first marker was added to the 320×180 project, Camtasia showed a dialog. It said markers and quizzes need editing dimensions of at least 640×480, and offered Auto Change or No. With No, "Markers cannot be used for a Table of Contents." I chose No.

**Not observed, so not claimed**

- How markers export from Camtasia (Smart Player table of contents, or MP4 chapters).
- Marker drag behavior and snapping specifics.
- What the Rename dialog looks like.
- Whether marker names are unique.

REA native UI observation is Mach-O-only, so it was not used. The marker tray did not expose its child elements to Windows UI Automation, so the chips were observed visually. No reference code or assets were used.

## TatsiaCraft behavior

**Data**

- `Project.Markers` holds markers with an ID, a frame-aligned time and a name.
- Names are trimmed, nonblank, at most 200 characters, and contain no control characters.
- Only one marker can sit on a given frame.
- Projects saved before 0.0.12 load with an empty marker list.
- `Engine.Length` (the duration) now includes markers. Preview, playhead range and export all extend to a later marker, and the export shows black after the last clip.

**Editing**

- The menus and shortcuts listed above add, remove, show or hide, and jump between markers.
- Clicking an empty spot in the tray adds a marker there, snapping to the playhead.
- New markers get the lowest unused "Marker N" name.
- A chip can be renamed with a double-click, the right-click Rename, or the Marker name field in Properties. Rename uses an owned dialog.
- A chip can be deleted with the right-click Delete, the Delete Marker button, or the Delete key while the marker is selected.
- Selecting a marker moves the playhead to it and clears the clip selection.
- Every edit is one undo checkpoint. Showing or hiding the tray is not an undoable edit.

**Snapping** (within 8 px; hold Shift to turn it off)

- Dragging a marker snaps to clip edges, other markers and the playhead.
- Dragging a clip snaps to markers too.
- Dragging the playhead snaps to markers.

**Export**

- Each marker before the export end becomes an MP4 chapter titled with the marker name. A chapter runs until the next marker or the end of the video.
- Exported pixels are unchanged.
- Unlike the reference, TatsiaCraft has no 640×480 minimum and shows no dialog. Chapters are written at any project size.

**Bug fixed**

Tool output was decoded with the console code page, which garbled Unicode in ffprobe results. `Engine.Run` now reads it as UTF-8.

**Still open**

- The marker thumbnail in Properties.
- Quizzes and the transcript tray.
- A Timeline Snapping > Markers toggle.
- Smart Player / HTML table-of-contents output.
- Ripple behavior for markers.

## Acceptance

`scripts/test-markers.ps1` runs 98 checks.

**Model**

- Frame alignment, naming, the one-marker-per-frame rule and sorting.
- Rename rules: trimming, Unicode, the unchanged case, and rejecting blank, overlong or multi-line names, atomically.
- Moving markers, including collisions.
- Next and previous marker.
- Duration extension.
- Rejecting invalid times, IDs and names in project files.
- Exact save and reopen, and loading older projects.
- Chapter metadata spans and escaping.

**Export**, checked by decoding the output with ffprobe and ffmpeg:

- No chapters without markers.
- Three chapters with Unicode, `=` and `;` titles, with correct start and end times.
- Unchanged pixels and duration.
- A later marker extends the export, with a black tail and no empty chapter.

**Editor**

- Tray visibility and the track offset it causes.
- Add, select and show the Marker properties panel.
- The chip's position.
- Undo checkpoints for add, rename, delete and Remove All, and their undo and redo.
- The Delete key.
- Jumping between markers.
- Snap targets and the 8 px threshold.
- Marker selection clearing on clip selection.
- Duration and playhead range extension.
- Menu wording and shortcut text.
- Saving markers from the editor.

The UI suite (`scripts/test-ui.ps1`, 75 checks) now expects the Jump To marker entries and renders markers in its snapshot.
