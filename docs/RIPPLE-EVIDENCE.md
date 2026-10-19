# Ripple Delete, ripple move and Separate Audio and Video (0.1.0)

## Reference observation

Target: installed Camtasia 26.2.2.19936 editor on Windows, 2026-10-18. It had an untitled project with a video-with-audio fixture clip on Track 1 and Track 2. Changes made during the observation were undone through Camtasia's own Undo button, and a UI Automation recount confirmed the original two clips were back.

- **Clip context menu (Track 1 clip).** The entries include:
  - Cut `Ctrl+X`, Copy `Ctrl+C`, Paste `Ctrl+V`, Duplicate `Ctrl+D`
  - Delete `Delete`, **Ripple Delete `Ctrl+Backspace`**
  - Hide Properties `Ctrl+2`, Group `Ctrl+G`
  - **Separate Audio and Video**, Edit Audio, Silence Audio `Shift+S`, Restore Audio `Shift+R`
  - Add Clip Speed, Extend Frame, Update Media, Convert to Placeholder, Add to Library
- **Edit menu.** Ripple Delete `Ctrl+Backspace` is listed after Delete, and Separate Audio and Video is in the Split group.
- **Separate Audio and Video.** Applied to the Track 1 clip:
  - Camtasia inserted a **new track directly above** the source track and placed an audio-only clip there.
  - The audio clip has the same name, start and length as the source.
  - The former Track 2 moved up to Track 3.
  - Both halves stayed selected; Properties read "fixture and others".
  - One Undo restored everything.
- **Not observed.** Ripple Delete and ripple move were not exercised, because they need multiple clips on one track and would have meant restructuring the reference project. Their behavior here follows the reference's documented workflow and is treated as an independent implementation:
  - Ripple Delete removes media and closes the gap.
  - Holding Shift while dragging moves the media together with the media after it on that track.

REA native UI observation is Mach-O-only, so it was not used. No reference code or assets were used.

## TatsiaCraft behavior

### Ripple Delete (`Ctrl+Backspace`; Edit menu; clip context menu)

- Removes the selected clips. Groups are handled as whole units.
- On each affected track, closes exactly the time those clips occupied:
  - A later clip moves left by the total length of deleted media before its start.
  - A clip that overlaps a deleted span moves only by the part removed before it.
- Other tracks and markers are unchanged.
- Locked tracks reject the operation atomically.
- Groups left with one member are pruned.
- One undo checkpoint.

### Ripple move (Shift-drag on a clip body)

- Moves the clip (or its selection or group unit) together with every clip on the same tracks that starts at or after the moved media. Followers' groups move whole.
- Ripple move keeps clips on their tracks and does not snap.
- Shift-click without dragging still toggles the selection, as in 0.0.9. Ctrl-click also toggles.
- Moves use the existing shared-delta move operation: clamped at 0, guarded against locked tracks, one undo checkpoint.

### Separate Audio and Video (Edit menu; clip context menu)

- Applies to video clips that have audio.
- Inserts a track above each source track. Higher tracks, and their hidden, locked and snap-off states, move up one.
- The new audio clip:
  - has the same media, timing, speed, gain, fades and audio effect;
  - joins the source's group;
  - has visual transform fields reset.
- The source keeps its visual settings and becomes video-only.
- Both halves are selected. One undo checkpoint.
- The new clip `Stream` field (`video` or `audio`) is saved with the project. Older projects load as combined clips.
- Export and live preview leave out the audio of video-only clips and the picture of audio-only clips, so each half can be hidden, moved or muted on its own.

### Not yet supported

Silence and Restore Audio, Stitch, Ripple Insert from the playhead, and a ripple toggle for other edits.

## Acceptance

`scripts/test-ripple.ps1` runs 76 checks.

- **Ripple Delete:**
  - same-track gaps, other tracks unchanged, multiple and multi-track deletions;
  - overlapping clips and deleted unions;
  - locked atomicity, unknown or empty selections, group pruning, markers unchanged.
- **Ripple move:**
  - follower selection, shared delta, left clamp and gaps kept;
  - grouped followers, multi-track selections, last clip, locked tracks.
- **Separate Audio and Video:**
  - which clips are eligible, where the new track goes, and that track states shift;
  - what the audio and video halves keep, and new identity;
  - separating twice does nothing; validation, save/reopen and loading older projects;
  - locked and 64-track limits, joining the group.
- **Separate Audio and Video in decoded export and preview:**
  - audio level and picture match the combined clip;
  - hiding the audio track silences the clip and keeps the picture;
  - hiding the video track keeps the audio and removes the picture;
  - moved separated audio is silent before its new start;
  - live preview matches the combined clip.
- **Editor:**
  - checkpoints and undo for Ripple Delete and Separate;
  - locked no-op, selecting both halves, text clip no-op;
  - menu order and shortcut text.
