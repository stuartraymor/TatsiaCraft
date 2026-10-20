# Favorites and Behaviors (0.1.4)

## Reference observation

Target: installed Camtasia 26.2.2.19936 on Windows, 2026-10-18. Everything added during the observation was undone with Camtasia's own Undo, and a UI Automation check confirmed the Behavior properties tab was gone again.

**Tool order.** Favorites, Media, Annotations, Visual Effects, Transitions, Animations, **Behaviors**, Cursor Effects, Audio Effects, Narration, Captions. The Favorites tooltip reads "**Favorites (F)** — Save useful video enhancements in one location for quick access."

**Favorites panel**
- A **Type** filter.
- Empty-state text: "Click the star icon to save callouts, effects, or other video enhancements to Favorites."
- Each preset tile shows a star on hover, with the tooltip "Click to save to Favorites."

**Behaviors panel**
- A tile grid with animated previews of the 13 behaviors listed in Modify > Add Behavior: Drifting, Explode, Fade, Fly In, Jump And Fall, Pop Up, Pulsating, Reveal, Scale, Shifting, Sliding, Tremble, Wiggle.
- The tile context menu has **Add to Selected Media**, **Add to Favorites**, Delete and Rename.

**Applying Fade to a clip**
- A "**Behavior Added**" notice appears.
- A Behaviors tab is added to Properties. It shows the behavior name with an on/off toggle, and **In / During / Out** tabs.
- The In tab has Style (Fade In), Movement (Linear) and Speed (83%).

**Not measured.** The During and Out tabs didn't respond to synthetic clicks. Reference motion curves, default timings and the Style/Movement options were not measured. No reference code or assets were used.

## TatsiaCraft behavior

### Favorites

- Every preset tile (Annotations, Visual Effects, Transitions, Animations, Behaviors, Audio Effects, Captions) has a star toggle with the reference tooltip.
- The right-click menu offers Add to/Remove from Favorites, plus Add to Selected Media for clip presets.
- The Favorites tool lists starred presets with a Type filter, or shows the reference empty-state text. `F` opens it.
- Favorites belong to the user, not the project. They are saved atomically to `%LOCALAPPDATA%\TasiaCraft\favorites.json`.

### Behaviors

All 13 reference behaviors are implemented: eight in 0.1.4 and the last five in 0.2.4. Each motion curve is an independent definition:

| Behavior | In / Out | During |
|---|---|---|
| Fade | linear alpha | — |
| Fly In | eased in from one canvas width left; out to the right | — |
| Pop Up | 0 to 100% with overshoot; shrinks out | — |
| Scale | eased 0 to 100%; shrinks out | — |
| Sliding | rises from a quarter canvas below with fade; exits upward | — |
| Pulsating | — | scale 100–106% every 0.8 s |
| Tremble | — | jitter of up to 3 px |
| Drifting | linear alpha | slow drift of ±2% canvas width |
| Explode | shrinks from 180% to 100% while fading in; bursts back to 180% while fading out | — |
| Jump And Fall | drops from 30% canvas height with decaying bounces; falls 60% of the canvas height while fading out | — |
| Reveal | grows from 60% to 100% while fading in; fades out | — |
| Shifting | — | gentle circular sway of ±2% canvas size every 2 s |
| Wiggle | — | tilts ±6° at 1.5 Hz (animated rotation; export uses a fixed-size rotate canvas) |

- **Applying:** click a tile, use its context menu, or use Modify > Add Behavior. Behaviors apply to the selected visual or text clips. Audio clips and locked tracks are refused.
- **Defaults:** In and Out are each 1 s, or a third of the clip if shorter. If In plus Out is longer than the clip, both shrink proportionally.
- **Properties:** each behavior gets an On toggle, In and Out durations (0–60 s) and Remove.
- Up to 8 behaviors stack per clip. Every edit is one undo checkpoint, and behaviors are saved with the project.
- **Rendering:** preview and export use the same formulas.
  - Export scales media per frame, padded to a fixed size so rotation and transform animation still work. Alpha uses fade filters, and position uses overlay expressions.
  - Text uses drawtext alpha, position and per-frame font size.

**Still open:** the Style, Movement and Speed options; custom, renamed or deleted presets; and Cursor Effects and Narration.

## Acceptance

`scripts/test-behaviors.ps1` runs 80 checks.

- **Curves:** every behavior's curve at key times; proportional shrinking of short phases; the On toggle; stacking.
- **Validation:** unsupported names, invalid times, the per-clip limit, null entries, text clips allowed; save/reopen and older projects.
- **Export and preview agreement:** pixel probes for Fly In, Fade In/Out, Pop Up overshoot, Scale and Sliding mid-animation; rotated and transform-animated clips; text Fade and Scale.
- **Editor:**
  - selection, audio-clip and locked-track guards; checkpoints; the "Behavior Added" notice;
  - the Properties fields, invalid-input rejection, toggle, undo and remove;
  - the panel catalog, the Modify menu, navigation order.
- **Favorites:** empty-state text, star tooltip, saving and reloading, Type filter, applying from Favorites, unstarring, the `F` key.
