# Silence/Restore Audio, Stitch Selected Media and Ripple Insert (0.1.1)

## Reference observation

Target: installed Camtasia 26.2.2.19936 on Windows, 2026-10-18.

- **Wording and shortcuts.** The clip context menu has **Silence Audio `Shift+S`** and **Restore Audio `Shift+R`** (captured during 0.1.0). The Edit menu has **Stitch Selected Media `Ctrl+Alt+I`** between Split All and Separate Audio and Video.
- **Silence Audio.** I applied it to the fixture clip, then read the Audio properties through UI Automation and a screenshot. Gain was still 100% (0.0 dB), and the clip showed audio level points. This suggests the reference silences through its audio-point envelope rather than the gain. That was not measured further. Restore Audio (`Shift+R`), which is safe to repeat, returned the clip to its original level line.
- **Not observed live.** Stitch and Ripple Insert would have meant restructuring the reference project. They follow the documented workflow:
  - Stitch joins adjacent media on a track.
  - Shift-dragging the playhead inserts time across tracks.

No reference code or assets were used. REA native UI observation is Mach-O-only.

## TatsiaCraft behavior

### Silence Audio and Restore Audio

- Toggle a per-clip `Silenced` flag on clips with playable audio. That covers video with audio and audio media, but not video-only halves or text.
- Export leaves the audio out and keeps the picture. Preview plays at zero volume.
- Gain, fades and effects are unchanged, so Restore is exact.
- The setting is saved, and older projects load unsilenced.
- Locked tracks reject the change atomically. Repeating the command does nothing.
- Range-based silencing from a timeline selection and audio-point envelopes are open.

### Stitch Selected Media

Joins selected clips only when they continue the same media exactly:

- they touch on the same track;
- their source time is continuous;
- speed, stream, settings and group membership match, and neither clip is animated.

The joined clip equals the original clip before the split. Stitching three or more pieces works in any selection order. The removed pieces are dropped from their groups. Stitching media that is not continuous, as the reference allows, is open: TatsiaCraft's clip model has no stitch points.

### Ripple Insert

- Shift-drag the playhead from its position. The drag distance, aligned to frames, becomes inserted blank time on every unlocked track.
- A clip that crosses the point is split, keeping source continuity and group membership.
- Clips at or after the point, and markers, move right. Locked tracks do not move.
- One undo checkpoint. An insert with nothing after it is a no-op. Exceeding the 24-hour limit is rejected atomically.

## Acceptance

`scripts/test-audiostitch.ps1` runs 68 checks.

- **Silence and Restore Audio:**
  - eligible clips, gain kept, repeat is a no-op;
  - decoded export is silent and the picture is unchanged;
  - Restore returns to the original level;
  - save/reopen, older projects, separated halves, locked atomicity.
- **Stitch:**
  - the joined clip is exactly the original;
  - split and stitched exports match frame for frame and in audio;
  - three-piece stitch in any order;
  - refusals for gaps, skipped source time, setting differences and different tracks, and for a single selected piece;
  - locked atomicity, groups.
- **Ripple Insert:**
  - splitting on every unlocked track, later clips moving, locked track unchanged;
  - marker handling, insert at zero and at a clip edge;
  - invalid amounts rejected;
  - decoded export: longer duration, unchanged picture before the point, delayed picture after it, silent gap, audio resumes.
- **Editor:**
  - checkpoints and undo for each command, and no-op paths;
  - rejection at the 24-hour limit;
  - Edit menu order and shortcut text.
