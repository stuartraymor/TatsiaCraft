# Cursor Effects (0.2.2)

## Reference observation

Target: installed Camtasia 26.2.2.19936 on Windows, 2026-10-19. I opened the tool from **More** and listed its tabs through UI Automation and screenshots. Nothing was applied.

The tool has three tabs: **Cursor**, **Left Click** and **Right Click**.

- **Cursor** effects: Cursor Color, Cursor Glow, Cursor Highlight, Cursor Isolation, Cursor Magnify, Cursor Motion Blur, Cursor Path Creator, Cursor Shadow, Cursor Smoothing, Cursor Spotlight, Kinetic Cursor.
- **Left Click** and **Right Click** effects: Burst 1–4, Rings, Ripple, Scaling, Scope, Sound, Target, Warp, Zoom. The left-click previews are red. The right-click previews are blue, with yellow rings.
- Properties has a cursor tab (`t-PropertiesPanelCursorProperties`).
- In the tool order, the tool sits between Behaviors and Audio Effects.

**Not observed:** effect parameters, timing and rendering; the reference recorder's cursor format. No reference code or assets were used.

## TatsiaCraft behavior

### Recording

- Screen recordings now capture the desktop **without** the cursor (gdigrab `-draw_mouse 0`).
- A per-monitor DPI-aware thread samples the cursor at about 60 Hz into `<recording>.cursor.json`. It records:
  - position relative to the captured virtual desktop;
  - shape: arrow, I-beam, hand, wait or hidden;
  - left and right clicks.
- Samples are timed from FFmpeg's `Output #0` line, so sync is approximate (measured latency compensation is open).
- On import, a recording's sidecar file is attached automatically.

**Fix:** finishing a recording no longer turns the Record button back into plain text.

### Rendering

- One drawing routine handles the cursor and its effects, in project coordinates. The mapping follows the clip's fit, scale, position, rotation, crop, transform animation, behaviors, speed and opacity.
- The live preview draws a layer just above each clip. Export renders the same drawing to a transparent QuickTime RLE overlay at project size and frame rate, then composites it right after that clip.
- Recordings made before 0.2.2 have the cursor baked into the video and no sidecar, so they are unchanged.

### Effects implemented (12)

Cursor:

- **Cursor Color:** recolors the cursor fill.
- **Cursor Glow:** a colored outline.
- **Cursor Highlight:** a translucent disc behind the cursor.
- **Cursor Shadow:** a 3 px offset shadow.
- **Cursor Smoothing:** a weighted average over ±window seconds.
- **Cursor Spotlight:** dims everything except a circle around the cursor.

Clicks, for each of left and right:

- **Burst 1:** radial dashes.
- **Rings:** an expanding, fading ring.
- **Target:** a shrinking circle with crosshairs.

Each click effect lasts 0.5 s of playback.

### Editing

- Each effect has a size where relevant, plus color and remove.
- **Cursor scale** (25–400%) and **cursor opacity** (0 hides the cursor) are saved per clip.
- Effects are applied from the tool, from Modify > Add Effect > Cursor Effects, or from Favorites. Each edit is one undo checkpoint.
- The tool has Cursor, Left Click and Right Click tabs. Click effects are named in full (for example "Left Click Rings") so favorites stay unique.

### Still open

- Cursor Isolation, Cursor Magnify, Cursor Motion Blur, Cursor Path Creator, Kinetic Cursor.
- Click effects Burst 2–4, Ripple, Scaling, Scope, Sound, Warp, Zoom.
- Custom cursor images, editing the cursor path, and exact reference visuals.

## Acceptance

`scripts/test-cursor.ps1` runs 60 checks.

- **Track:** interpolation, hidden cursor, smoothing, save/load/reload, invalid or missing data, live recorder sampling and time offset.
- **Mapping:** full frame, scale, rotation, position, crop exclusion.
- **Pixels in export and preview:** default cursor, opacity 0, cursor scale, Highlight, Spotlight, Color, Shadow, Glow, Left Click Rings (with expiry), right/left separation, Target, a scaled clip, click timing at 2× speed.
- **Validation:** unsupported names, sizes, colors, scale and opacity limits, effects without cursor data; save/reopen and older projects.
- **Editor:** guards, checkpoints, duplicate rejection, Properties, rejecting an invalid scale, undo, navigation order, the tabs and their lists, the Modify menu, sidecar attach on import, Record button state.
