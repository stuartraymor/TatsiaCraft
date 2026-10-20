# Camtasia workflow reproduction plan

Target: installed Camtasia 26.2.2.19936 on Windows. Goal: independently implement matching features, placement, workflow and output, then validate each against controlled observations. Current status: partial development preview. Full parity is not established.

## Delivery cycle for every completed revision

1. Inventory the feature's entry points, state, gestures, shortcuts, settings and errors in the reference. Use REA for supported shipped-artifact questions and window-specific runtime observation for Windows behavior. Record observations, inferences and unknowns separately.
2. Write acceptance checks, including preview/export agreement, save/reopen, undo/redo and invalid inputs where relevant. Reuse existing evidence only when its exact target and scope match.
3. Implement an independent increment in the existing architecture. Do not add inert controls to imply feature completion. Reference assets and captured user media stay outside the source repository.
4. Build, run focused regression tests, exercise the actual UI and compare results. Record partial coverage and unresolved differences.
5. Install the versioned build, verify payload hashes and startup, then smoke-test the installed workflow. Preserve existing versions and unsaved projects.
6. Commit, push, tag and publish a GitHub revision with source and executable assets. Verify the remote commit, release and asset identity. Follow VERSION rollover rules.
7. Update this ledger and select the next unmet acceptance check. Failed or untested work remains open.

## Feature/workflow inventory

Every row needs a detailed sub-inventory before it can be marked complete; this table is a roadmap, not a claim to have discovered every reference feature.

| Order | Workflow | Implementation/evidence status | Required validation |
|---|---|---|---|
| 1 | Workspace, Home, menu/navigation, version display, detached canvas, zoom | Menus, tooltips, tool order, caption, transport/timeline toolbars and icons follow observed reference wording and layout (0.0.11); reference-only tools not yet implemented; pixel styling unmeasured | Every control/state, matching window/DPI captures, shortcuts, focus, small/large windows |
| 2 | Canvas select, move, resize, rotate, crop, pan, guides, align, fit | Move/resize/rotate exist; measured crop and hand-pan increments implemented; basic alignment/snapping guides implemented; advanced transforms and exact interaction parity open | Native-size geometry, every handle/modifier, off-canvas display, undo, saved data and exported pixels |
| 3 | Import, media bin, sort/filter/search, library, favorites, templates | Limited original implementation | Formats, duplicates, missing/relinked media, metadata, drag/drop and persistence |
| 4 | Timeline move/trim/split/delete, ranges, snapping, zoom, track controls | Basic edits/track visibility/lock exist | Exact boundaries, frame rounding, same-track overlaps, disabled states, shortcut equivalence |
| 5 | Multiple selection, grouping, ripple editing, markers, AV separation | Flat grouping (0.0.9-0.0.10) and timeline markers with tray, navigation, snapping and MP4 chapters (0.0.12); Ripple Delete, ripple move and Separate Audio and Video (0.1.0); Silence/Restore Audio, Stitch and Ripple Insert (0.1.1); audio points, range silencing and non-continuous stitches open | Selection and group state, linked timing, ripple propagation, undo/save/export |
| 6 | Animation/keyframes/easing and Behaviors | Simple transform endpoints; all 13 Behaviors with In/Out timing (0.1.4, 0.2.4); keyframes and Style/Movement options open | All catalogs, interpolation, parameter ranges, overlapping effects and rendered output |
| 7 | Visual effects and transitions | Small independent subset | Inventory every preset and adjustable parameter; alpha, duration, overlaps, performance |
| 8 | Text, callouts, shapes, annotations | Basic text and three shapes | Fonts/colors/layout, styling, rotation, resize, object grouping and export |
| 9 | Audio editing/effects/meters | Gain/fades/proxies; Silence/Restore (0.1.1); audio points with range silencing (0.1.2); meters, ducking and noise tools open | Sample alignment, normalization, envelope points, noise processing, meters, multitrack mixes |
| 10 | Recorder screen/region/window, camera, mic, system audio | Basic FFmpeg capture; Narration voice recording with meter and timeline sync (0.2.1) | Pause/resume, simultaneous sources, device loss, long sessions, separate tracks, timing |
| 11 | Cursor recording metadata and cursor effects | Cursor path/shape/click recording and 12 cursor/click effects (0.2.2); remaining effects and latency compensation open | Metadata capture, highlighting, smoothing, click effects and zoomed output |
| 12 | Captions/transcription and text workflows | SRT import only | Editing/timing/styles, caption formats, recognition quality, language and offline behavior |
| 13 | Projects, autosave/recovery, portable media, compatibility | JSON projects; bounded Windows lock recovery and transactional Save As tested; native formats open | Atomic writes, missing/corrupt files, legacy data, recovery, media embedding, .tscproj/.trec research |
| 14 | Export, formats/profiles/quality/hardware/queue | Basic MP4 | All settings, duration/frame/audio/pixels, cancel/failure/source preservation, batch/retry |
| 15 | Interactive hotspots/quizzes/Smart Player | Open | Browser/player behavior, metadata, accessibility and distribution |
| 16 | AI tools, integrations, cloud and external services | Open; service behavior cannot be inferred from local code alone | Current actual availability, local/service alternatives, quality and failure modes |
| 17 | Accessibility, localization, preferences and help | Partial | Keyboard-only flows, UIA, screen readers, persisted preferences, locale and DPI |
| 18 | Install/update/uninstall and distribution | Per-user Setup.exe with bundled FFmpeg, in-place delta updates, rollback, Apps & Features uninstall, file association and in-app update check (0.2.3); code signing open | Clean install, hash identity, startup, shortcuts, version coexistence, safe user data handling |
| 19 | Exact visual comparison and performance | Not established | Matched-size state captures, declared visual acceptance, playback drops/latency, memory, export throughput/quality |

## Revision ledger

- 0.0.1: shared version identity, top-left version display and required GitHub release workflow; 28 canvas/title checks passed.
- 0.0.2 increment: four-edge/corner canvas cropping through preview, serialized projects and export; independent crop controls and original iconography; versioned install and installed startup verification. 37 crop acceptance checks passed. Actual mouse drag from uncropped source persisted CropLeft25%, Scale100%, Position X/Y0; one undo restored the original visible source. Pan/advanced-transform and full crop interaction parity remain open.
- 0.0.3 increment: canvas hand/pan mode. Observed reference hand mode hides selection grips; a 50×30 logical-pixel drag moves the displayed project by 50×30 while Position X/Y remain0, Scale100% and Width320. Choosing Fit recenters. 33 checks passed for viewport-only displacement, unchanged project/export data, hidden grips in hand mode, retained pan across refresh/detach, reset on a different project and no undo checkpoint. Actual mouse testing reproduced the displacement and Fit reset, including selecting an already-selected Fit entry. This does not establish wheel zoom anchors, modifier shortcuts, saved viewport state or every high-zoom scroll interaction.

- 0.0.4 increment: cropped-object resize anchoring. The measured reference corner and edge gestures, acceptance criteria and REA coverage boundaries are recorded in [RESIZE-EVIDENCE.md](RESIZE-EVIDENCE.md). Independent tests cover every handle, rotated geometry, fitted/native sizes, pointer offsets, clamps, actual preview/export pixels and save/reopen.
- 0.0.5 increment: off-canvas editing dimming. [OFF-CANVAS-EVIDENCE.md](OFF-CANVAS-EVIDENCE.md) records the reference observation and qualitative limits. 44 checks passed for four-edge overflow, bright interior/grips, viewport changes, project-data preservation and undimmed frame/video exports. This does not establish an exact shade color/opacity match.

- 0.0.6 increment: fixed-aperture crop source movement, canvas/active-layer edge and center snapping, transient alignment guides and six explicit alignment commands. [CANVAS-MOVE-ALIGN-EVIDENCE.md](CANVAS-MOVE-ALIGN-EVIDENCE.md) records observed behavior, independent choices and limits. 286 crop-movement and 68 alignment checks cover preview/export, save/reopen, source clamps, visible bounds, threshold across zoom, guide export exclusion and editor undo/redo.

- 0.0.7 increment: anchored resize snapping with the shared canvas setting, active visual layer targets and transient guides. [RESIZE-SNAP-EVIDENCE.md](RESIZE-SNAP-EVIDENCE.md) records the measured enabled/disabled side-handle comparison and remaining limits.445 checks cover every handle under cropped/rotated/native/fitted geometry, source/anchor preservation, zoom thresholds, preview/export and save/reopen.

- 0.0.8 increment: project-save robustness repair. [PROJECT-SAVE-EVIDENCE.md](PROJECT-SAVE-EVIDENCE.md) records the original UI failure, controlled pre-fix sharing violation, bounded atomic-commit retry and failed Save As state protection. 60 checks cover real temporary/permanent locks, original bytes, cleanup, Unicode paths, source protection, save/reopen/export and editor destination/dirty/title state. No reference save internals are claimed.

Next unmet canvas acceptance checks: rotated/animated reference interactions, expanded multi-selection gestures and exact guide/handle styling. Expand command/context-menu inventory and timeline selection/ripple workflows next. Project-save crash recovery, concurrent multi-process saves and network-filesystem behavior remain open. No entire workstream is marked complete by these increments.

Crop-specific residuals: modifier keys, full rotated/animated reference resize parity, exact reference handle styling, text crop, perspective and animated crop boundaries. UI crop drags retain at least two source pixels; numeric/serialized crops permit one. Preview uses fractional boundaries while export rounds boundaries to whole source pixels; subpixel edge parity is not claimed.

## Current evidence boundary

Prior retained REA identity evidence binds the reference launcher SHA-256 `ef9594e005d64ea2aee2f2ddc1d96f53cffa8c354a4c47d01a781152721537bc`. Metadata identified WPF UI components, a mixed C++/CLI editor boundary and separate timeline/property assemblies. Metadata inventory does not prove behavior.

Current `inspect_managed_members` on WPFPropertiesPanel.dll returned evidence `ev_af13327c35ac2664a56279231be655818fc3e3b84567821074ac9af9a32d5bf5`, coverage unavailable: its representation exceeded the 64 MiB budget. No crop implementation is inferred from that failure. InteropDependencies.dll native-boundary inventory (`ev_73a4325e2fb9ae6fffa46c610a4050f54639c26d6deea7d52fe8d5fd57810d83`) found no P/Invoke/native method declarations in that assembly; this does not imply the application lacks native code. These bounded results are retained locally, not repeatedly requested.

Native Ghidra analysis of CamtasiaFilters.dll (SHA-256 `f76b778263bbc731b4bef554bb744bc220ff34d1153c53a2d9d88d03870b224f`) completed. Defined-string regex search `crop|transform|pan` returned transform-filter/input-pin/output-pin RTTI and CompanyName, with no crop string among these matches: evidence `ev_691f1728d984ca5900c06c8c5ed48c91f368cec97a2c3db534a266314510b25d`. This bounded string result does not locate the crop implementation or prove crop is absent from the module. No proprietary pseudocode or assets were used in the independent crop implementation.

Windows live crop observation: reference at 100% view zoom, 320×180 fixture, Scale100%, Position X/Y0. Dragging left crop handle 20 displayed logical pixels right (40 source pixels on this DPI setup) changed Width320→280, kept right visible edge fixed and Position X/Y0, and did not stretch the remaining image. This directly supports source-relative clipping while retaining the original transform pivot for this unrotated case. Corners, rotation, text, animated crops, minimum size and modifiers require separate observations.


- 0.0.9 increment: flat multi-selection/grouping, Ctrl+A/G/U, collapsed root timeline units, Open group/Main Timeline member context, unit canvas/timeline moves, batch clipboard/delete and group-aware undo/save. [GROUPING-EVIDENCE.md](GROUPING-EVIDENCE.md) records live reference observations and retained REA coverage limits. Nested groups, local group timelines, group effects/resize/rotation, marquee and exact modifier/layout parity remain open. Duplication retains the independent editor's after-selection placement; observed reference duplicate track placement is not reproduced yet. Member editing retains absolute project time/tracks. No full workstream parity is claimed.


- 0.0.10 increment: group naming through properties, Edit and timeline context menu, owned Rename/Cancel dialog, guarded Unicode names, no-op/invalid state protection and one-checkpoint undo/save. [GROUP-RENAME-EVIDENCE.md](GROUP-RENAME-EVIDENCE.md) distinguishes the observed reference menu entry from independent dialog/validation choices. 37 additional checks (grouping suite223) verify name-only mutation, preview/export preservation and error/undo/save behavior. Nested hierarchy, group transforms/effects and exact reference rename-dialog/shortcut behavior remain open.


- 0.0.11 increment: interface wording, menu structure, tooltips and original vector iconography follow the live reference UI Automation inventory (1,478 nodes, all six menus expanded without invoking them) and a matched-size screenshot. [UI-WORDING-EVIDENCE.md](UI-WORDING-EVIDENCE.md) lists the reproduced wording, the reference-only entries left out until they are implemented, and the limits. 75 checks cover menus, tooltips, caption, time formats, navigation and the new shortcuts. REA native UI observation is Mach-O-only and was not used. Pixel-level icon/layout parity and the empty Properties placeholder remain open.

- 0.0.12 increment: timeline markers. [MARKER-EVIDENCE.md](MARKER-EVIDENCE.md) records the live reference tray, naming, properties, context menu, shortcuts, duration extension and the 640x480 table-of-contents dialog, and separates these from independent choices (MP4 chapters at any size, snapping rules, the rename dialog). 98 checks cover the model, decoded chapter export, the editor and menus. The marker thumbnail, quizzes and transcript, the Markers snapping toggle and the Smart Player table of contents remain open.

- 0.1.0 increment: Ripple Delete, Shift-drag ripple move, and Separate Audio and Video with independently playable stream halves. [RIPPLE-EVIDENCE.md](RIPPLE-EVIDENCE.md) records the observed context/Edit menu entries and the live separation result (new track directly above, both halves selected, one undo). Ripple Delete and ripple move follow the documented workflow and were not measured live. 76 checks cover decoded export, preview and editor behavior.

- 0.1.1 increment: Silence/Restore Audio, Stitch Selected Media and playhead Ripple Insert. [AUDIO-STITCH-EVIDENCE.md](AUDIO-STITCH-EVIDENCE.md) records the observed menu wording and shortcuts, and that reference Silence leaves Gain at 100% (it uses audio points). TatsiaCraft uses a separate silenced flag, stitches continuous media only, and splits clips that cross a ripple insert. 68 checks.

- 0.1.2 increment: audio points stored in source time, a green/red playhead time range, and range Silence/Restore. Also fixes an existing export bug that corrupted audio for clips with both a source offset and a later timeline start. [AUDIO-POINTS-EVIDENCE.md](AUDIO-POINTS-EVIDENCE.md). 69 checks.


- 0.1.3 increment: bounded atomic export commit recovery, reusing the save retry policy and preserving existing destination bytes on permanent errors. [EXPORT-COMMIT-EVIDENCE.md](EXPORT-COMMIT-EVIDENCE.md) records the observed resize/export failure and independent repair. 24 real-lock/decoded-output checks cover commit status, cleanup, source/model preservation and invalid destinations. Export profiles/queue/cancellation during finalization, network filesystems and crash recovery remain open.

- 0.1.4 increment: Favorites tool and 8 Behaviors. [BEHAVIORS-EVIDENCE.md](BEHAVIORS-EVIDENCE.md). 80 checks.


- 0.1.5 increment: Media Bin Details, Select Unused Media and Delete Unused Media, using all timeline references and preserving source files. [MEDIA-BIN-EVIDENCE.md](MEDIA-BIN-EVIDENCE.md) distinguishes TechSmith's documented menu workflow from independent Details fields and current reference-runtime unknowns. 46 checks cover actual Details UI, selection/error/no-op/busy states, undo/redo/save, missing/aliased sources and preview/decoded export preservation. Exact26.2.2 details/layout, metadata refresh and batch insertion remain open.


- 0.1.6 increment: Media Bin sort direction and stable visible selection through view changes/refresh, with stale selection cleared for other projects and filtered-out entries. [MEDIA-SORT-EVIDENCE.md](MEDIA-SORT-EVIDENCE.md) separates documented direction controls from independent collation/selection policy. 29 additional checks (media-bin75) cover actual controls, Unicode/ties, no model/dirty/undo mutation, Relink eligibility and preview/decoded export. Exact reference26.2.2 collation/options/layout and broader keyboard/focus parity remain open.

- 0.1.7 increment: independent Media Bin source preview via double-click/context, with play/pause, stop, seek and disposal; explicit context insertion retains the former insertion workflow. [MEDIA-PREVIEW-EVIDENCE.md](MEDIA-PREVIEW-EVIDENCE.md) separates historical TechSmith documentation from current reference runtime unknowns and independent modal/paused choices. 78 added checks (media-bin153) cover actual WPF handlers, decoded image pixels, opened audio/video decoders, transport, corrupted-source reporting, unchanged timeline/undo/save/main preview/export and guarded insertion. Native gesture/layout/autoplay, full-screen/volume controls and audible quality remain open.

- 0.1.8 increment: bidirectional Media Bin/timeline context navigation. [MEDIA-NAVIGATION-EVIDENCE.md](MEDIA-NAVIGATION-EVIDENCE.md) distinguishes documented commands from independent group-unit/union/no-op policies. 38 additional checks (media-bin191) cover actual handlers, distant timeline scrolling, filtered bins, split-stream and alias identities, hidden/locked/grouped/missing references, unchanged playback/head/history/project/save/preview/export. Current native focus, selection rules and exact scrolling/layout remain open.

- 0.1.9 increment: sequential batch insertion through Add to Timeline at Playhead in one undo step. [MEDIA-BATCH-EVIDENCE.md](MEDIA-BATCH-EVIDENCE.md) records the documented sequential workflow and independent visible-order/overlap/configuration policies. 39 added checks (media-bin230) cover pure validation, actual context handlers, ascending/descending order, atomic failures, source preservation, existing group/clip preservation, selection, undo/redo/save and preview/decoded export agreement. Multi-item dragging, frame rounding and exact native insertion policy remain open.

- 0.1.10 increment: multiple Media Bin drag/drop via the validated batch pipeline, with drag threshold, deferred click selection, project-scoped payloads and timeline coordinate mapping. [MEDIA-DRAG-EVIDENCE.md](MEDIA-DRAG-EVIDENCE.md) records scope and native evidence limits. 40 additional checks (media-bin270) cover source-owned WPF press/move/drop wiring with a substituted drag executor, stale/malformed/locked/missing/busy guards, ordering/time/track/cancel, undo/redo/save and preview/export. Physical OS drag loop, exact current reference gestures/modifiers/snapping and cross-process drops remain open.

- 0.1.11 increment: source-preview failure latch, typed playback errors, safe image stream loading and Retry. [PREVIEW-FAILURE-EVIDENCE.md](PREVIEW-FAILURE-EVIDENCE.md) records the pre-fix false Playing state and failed-image file lock. 24 added checks (media-bin294) cover persistent errors/disabled commands, corrupt and asynchronous decoder failures, repaired/removed/replaced sources, cleanup and unchanged project/main preview/export/save. This is an independent defect repair; exact reference retry/error UI remains open.

- 0.1.12 increment: functional Timeline Snapping target controls for Media Edges, Playhead and Markers, with shared nearest-target selection for clip/group, marker and playhead workflows. [TIMELINE-SNAPPING-EVIDENCE.md](TIMELINE-SNAPPING-EVIDENCE.md) separates historical settings documentation from independent thresholds/session/priority/modifier policies. 45 added checks (media-bin/integration339) cover options, actual menu handlers, group/track exclusion, zoom/ties, undo/project persistence and unchanged save/preview/export. Exact current reference modifiers, snap-line styling, trim/end-edge and animation/quiz snapping remain open.

- 0.2.0 increment: leading/trailing-bound snapping for moving timeline clips and group selections. [TIMELINE-MOVE-SNAP-EVIDENCE.md](TIMELINE-MOVE-SNAP-EVIDENCE.md) separates the documented beginning/end workflow from independent bounding-span/tie policies. 39 added checks (media-bin/integration378) cover both directions/endpoints, nearest/tie/zoom/bypass, invalid inputs, source/target flags, gapped groups, actual editor move operations, locks, undo/redo/save and preview/export. Current reference priorities, physical gestures and snap-line styling remain open; trimming and individual interior group-edge snapping are not added.

- 0.2.1 increment: Narration tool with microphone selection, live meter, input level, timeline mute, synced voice recording placed on a new track, and a saved script. [NARRATION-EVIDENCE.md](NARRATION-EVIDENCE.md). 40 checks.

- 0.2.2 increment: separate cursor recording and the Cursor Effects tool. [CURSOR-EFFECTS-EVIDENCE.md](CURSOR-EFFECTS-EVIDENCE.md). 60 checks.

- 0.2.3 increment: consumer installer and updater. See [INSTALLATION.md](INSTALLATION.md). About 300 Setup checks plus update-logic checks.

- 0.2.4 increment: Explode, Jump And Fall, Reveal, Shifting and Wiggle, with animated rotation. [BEHAVIORS-EVIDENCE.md](BEHAVIORS-EVIDENCE.md). 108 behavior checks.
