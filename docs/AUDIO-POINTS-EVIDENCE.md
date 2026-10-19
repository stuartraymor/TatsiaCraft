# Audio points and range Silence/Restore (0.1.2)

## Reference observation

Target: installed Camtasia 26.2.2.19936 on Windows, 2026-10-18.

- **Audio level line.** The UI Automation tree exposes `rootAudioLevelEnvelope` and `rootAudioLevelKeyframe` elements over each timeline clip. On screen they appear as a level line with a point marker at the clip start.
- **Silence Audio leaves Gain alone.** After Silence Audio, Gain in the Audio properties still read 100% (0.0 dB). This shows the reference silences through this envelope, not the gain (0.1.1 observation).
- **Menu wording.** Modify > Remove > **All Audio Points**, from the 0.1.0 menu inventory.
- **Range selection.** The playhead has **green start** and **red end** thumbs (`SelectionStartThumb` / `SelectionEndThumb`) for selecting a time range.
- **Not measured.** Gestures for adding and dragging audio points, the envelope's numeric range and drawing scale, and exactly how Silence Audio places points for a range. No reference code or assets were used.

## TatsiaCraft behavior

### Model

- `Clip.AudioPoints` holds points with a level from 0 to 100% at a **source-media time**.
- Source time is used for the same reason as `Clip.In`: points stay with the audio through trims, splits, stitches, ripple inserts and speed changes.
- The level interpolates linearly between points and holds the first or last level outside them. A clip with no points plays at full level.
- The envelope multiplies the clip gain and fades. Silence/Restore without a range still uses the whole-clip flag from 0.1.1.
- Projects saved before this version load with no points.
- Separating audio and video gives the audio-only half the points and clears them from the video-only half.

### Editing

Each audio clip draws a green level line. Hidden for text, video-only halves and whole-clip silenced clips.

- **Add:** double-click the line to add a point at the current level.
- **Move:** drag a point to change its time and level. Points cannot pass their neighbours.
- **Delete:** right-click a point > Delete Audio Point.
- **Remove all:** Modify > Remove > All Audio Points works on the selection, or on every clip when nothing is selected.
- Locked tracks show the line but cannot be edited. Each edit is one undo checkpoint.

### Range selection

- Drag the green or red handle beside the playhead to select a time range. The range is aligned to frames and shaded.
- Clicking the timeline, or Deselect All, clears it.

### Silence and Restore with a range

**Silence Audio (Shift+S)** with a range:

- applies to the selected clips that overlap the range, or to every overlapping audio clip when nothing is selected;
- adds points that drop to 0% across the range, with one-frame ramps, keeping the existing level on either side.

**Restore Audio (Shift+R)**:

- with a range, removes the points in the range, so the level follows the points around it;
- without a range, clears the whole-clip silence and all of that clip's points.

### Export

FFmpeg applies a per-frame volume expression in source time.

### Export bug fixed

Audio clips that had both an In offset and a later timeline start were exported with a corrupt audio stream (start time about 1.9e14 s, about 0.02 s long). This existing bug was found while testing. The cause is FFmpeg's `amix` producing a bad first timestamp for trimmed, delayed inputs. The mixed audio is now retimed from the sample count. A hand-run check confirmed that the delay is preserved: silent until 0.5 s, the second clip starting at 2.7 s.

## Acceptance

`scripts/test-audiopoints.ps1` runs 69 checks.

- **Model:**
  - interpolation, holds outside the points, replacing a point, clamps, moves held between neighbours, removal;
  - rejecting levels above 100%, NaN, out-of-order points, points on text, null points;
  - exact save/reopen, older projects.
- **Source time:**
  - split halves keep their timeline levels, stitch restores the envelope;
  - left trims, 2x speed, ripple insert.
- **Range:**
  - silent inside, full level outside, starting silent at the range's first frame;
  - four points with ramps, a range at the clip start;
  - existing level kept on both sides;
  - restore back to the surrounding level, and clearing.
- **Decoded export (loudness by time window):**
  - full level before the points, silence after a 0% point, a linear ramp at about 50% halfway;
  - range silence inside the range only, duration unchanged;
  - In and start offsets (this is the case that exposed the export bug);
  - envelope multiplied by gain.
- **Editor:**
  - add and delete checkpoints with undo, locked tracks blocked;
  - line and point handles drawn, none on text;
  - range Silence across overlapping clips with no selection, range Restore on the selection only;
  - range clearing, whole-clip Restore clearing points, Remove All Audio Points, menu wording;
  - range handles and shading, reversed range ordering.
