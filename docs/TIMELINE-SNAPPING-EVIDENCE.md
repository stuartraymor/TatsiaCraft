# Timeline snapping controls

## Evidence and boundary

TechSmith's [Studio 8 Help](https://assets.techsmith.com/docs/pdf-camtasiastudio/camtasia_studio_8_help_file.pdf), page 93, documents View > Timeline Snapping and selectable target options. Its historic modifier is Ctrl. This does not establish the current 26.2.2 settings layout, modifier, thresholds or priority.

Retained REA partial WPFSharedFiles.dll member evidence `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c` is reused; static selection metadata does not establish snapping behavior. Existing Windows evidence and capture limitations are retained; identical failed native probes are not repeated. No proprietary code/assets are used.

## Acceptance before implementation

- Add checked View > Timeline Snapping > Media Edges, Playhead, Markers controls. Each controls real target generation, independently of canvas snapping and per-track snapping flags. No inert animation/quiz options.
- Settings belong to the editor session, default on, survive refresh/undo/project switches within that editor, and do not enter project JSON or change dirty/undo/redo/head/playback/source files.
- Clip/group movement uses nearest enabled target within the existing strict eight-display-pixel threshold. Exclude moving clips and target tracks whose snapping is disabled; source-track flags and existing Shift bypass still apply. Keep zero as the timeline boundary.
- Marker movement respects enabled clip/marker/playhead targets, excluding itself. Playhead movement respects enabled clip/marker targets without snapping to its own current position. Marker creation near the playhead respects the Playhead setting. Preserve frame alignment and existing edit checkpoints.
- Test all option combinations, hidden/locked/disabled tracks, multiple/group selections, zoom threshold/ties/invalid inputs, actual menu commands and editor integration, unchanged project/save/main preview/export. Do not claim new physical gesture or reference priority/modifier parity.
- Run release gates, install, test installed workflow, publish and verify remote source/commit/tag/asset identity.

## Independent choices and gaps

Nearest target with stable tie order, eight logical-display-pixel threshold, session-only persistence, zero-boundary behavior and retained Shift bypass are independent policies. Current reference modifier/priority/layout and timeline snap-line styling, trimming/end-edge snapping, animation/quiz targets and saved preferences remain open.

## Focused result

45 additional checks pass (MediaBinTest339). They cover every option combination, hidden/locked/disabled target tracks, moving clip/marker exclusions, stable ties and nearest targets, zoom thresholds/bypass/invalid inputs, actual menu commands and the same helpers wired to clip/group, marker and playhead gestures. Session settings survive refresh, project instance switches and undo/redo without entering saved project data. Actual preview and decoded export remain identical. Physical pointer gestures and current reference runtime semantics remain pending.
