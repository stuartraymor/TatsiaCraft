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
| 1 | Workspace, Home, menu/navigation, version display, detached canvas, zoom | Basic implementation; version and detached zoom tested; detailed styling differs | Every control/state, matching window/DPI captures, shortcuts, focus, small/large windows |
| 2 | Canvas select, move, resize, rotate, crop, pan, guides, align, fit | Move/resize/rotate exist; measured crop and hand-pan increments implemented; guides/advanced transforms and exact interaction parity open | Native-size geometry, every handle/modifier, off-canvas display, undo, saved data and exported pixels |
| 3 | Import, media bin, sort/filter/search, library, favorites, templates | Limited original implementation | Formats, duplicates, missing/relinked media, metadata, drag/drop and persistence |
| 4 | Timeline move/trim/split/delete, ranges, snapping, zoom, track controls | Basic edits/track visibility/lock exist | Exact boundaries, frame rounding, same-track overlaps, disabled states, shortcut equivalence |
| 5 | Multiple selection, grouping, ripple editing, markers, AV separation | Mostly open | Selection and group state, linked timing, ripple propagation, undo/save/export |
| 6 | Animation/keyframes/easing and Behaviors | Simple transform endpoints only | All catalogs, interpolation, parameter ranges, overlapping effects and rendered output |
| 7 | Visual effects and transitions | Small independent subset | Inventory every preset and adjustable parameter; alpha, duration, overlaps, performance |
| 8 | Text, callouts, shapes, annotations | Basic text and three shapes | Fonts/colors/layout, styling, rotation, resize, object grouping and export |
| 9 | Audio editing/effects/meters | Basic gain/fades/proxies | Sample alignment, normalization, envelope points, noise processing, meters, multitrack mixes |
| 10 | Recorder screen/region/window, camera, mic, system audio | Basic FFmpeg capture | Pause/resume, simultaneous sources, device loss, long sessions, separate tracks, timing |
| 11 | Cursor recording metadata and cursor effects | Open | Metadata capture, highlighting, smoothing, click effects and zoomed output |
| 12 | Captions/transcription and text workflows | SRT import only | Editing/timing/styles, caption formats, recognition quality, language and offline behavior |
| 13 | Projects, autosave/recovery, portable media, compatibility | JSON projects; native formats open | Atomic writes, missing/corrupt files, legacy data, recovery, media embedding, .tscproj/.trec research |
| 14 | Export, formats/profiles/quality/hardware/queue | Basic MP4 | All settings, duration/frame/audio/pixels, cancel/failure/source preservation, batch/retry |
| 15 | Interactive hotspots/quizzes/Smart Player | Open | Browser/player behavior, metadata, accessibility and distribution |
| 16 | AI tools, integrations, cloud and external services | Open; service behavior cannot be inferred from local code alone | Current actual availability, local/service alternatives, quality and failure modes |
| 17 | Accessibility, localization, preferences and help | Partial | Keyboard-only flows, UIA, screen readers, persisted preferences, locale and DPI |
| 18 | Install/update/uninstall and distribution | Versioned installer increment under test | Clean install, hash identity, startup, shortcuts, version coexistence, safe user data handling |
| 19 | Exact visual comparison and performance | Not established | Matched-size state captures, declared visual acceptance, playback drops/latency, memory, export throughput/quality |

## Revision ledger

- 0.0.1: shared version identity, top-left version display and required GitHub release workflow; 28 canvas/title checks passed.
- 0.0.2 increment: four-edge/corner canvas cropping through preview, serialized projects and export; independent crop controls and original iconography; versioned install and installed startup verification. 37 crop acceptance checks passed. Actual mouse drag from uncropped source persisted CropLeft25%, Scale100%, Position X/Y0; one undo restored the original visible source. Pan/advanced-transform and full crop interaction parity remain open.
- 0.0.3 increment: canvas hand/pan mode. Observed reference hand mode hides selection grips; a 50×30 logical-pixel drag moves the displayed project by 50×30 while Position X/Y remain0, Scale100% and Width320. Choosing Fit recenters. 33 checks passed for viewport-only displacement, unchanged project/export data, hidden grips in hand mode, retained pan across refresh/detach, reset on a different project and no undo checkpoint. Actual mouse testing reproduced the displacement and Fit reset, including selecting an already-selected Fit entry. This does not establish wheel zoom anchors, modifier shortcuts, saved viewport state or every high-zoom scroll interaction.

- 0.0.4 increment: cropped-object resize anchoring. The measured reference corner and edge gestures, acceptance criteria and REA coverage boundaries are recorded in [RESIZE-EVIDENCE.md](RESIZE-EVIDENCE.md). Independent tests cover every handle, rotated geometry, fitted/native sizes, pointer offsets, clamps, actual preview/export pixels and save/reopen.
- 0.0.5 increment: off-canvas editing dimming. [OFF-CANVAS-EVIDENCE.md](OFF-CANVAS-EVIDENCE.md) records the reference observation and qualitative limits. 44 checks passed for four-edge overflow, bright interior/grips, viewport changes, project-data preservation and undimmed frame/video exports. This does not establish an exact shade color/opacity match.

Next unmet canvas acceptance checks: source movement inside a crop and align/snap guides. Then expand the command/context-menu inventory and timeline selection/ripple workflows. No entire workstream is marked complete by these increments.

Crop-specific residuals: source movement within a crop, modifier keys, full rotated/animated reference resize parity, exact reference handle styling, text crop, perspective and animated crop boundaries. UI crop drags retain at least two source pixels; numeric/serialized crops permit one. Preview uses fractional boundaries while export rounds boundaries to whole source pixels; subpixel edge parity is not claimed.

## Current evidence boundary

Prior retained REA identity evidence binds the reference launcher SHA-256 `ef9594e005d64ea2aee2f2ddc1d96f53cffa8c354a4c47d01a781152721537bc`. Metadata identified WPF UI components, a mixed C++/CLI editor boundary and separate timeline/property assemblies. Metadata inventory does not prove behavior.

Current `inspect_managed_members` on WPFPropertiesPanel.dll returned evidence `ev_af13327c35ac2664a56279231be655818fc3e3b84567821074ac9af9a32d5bf5`, coverage unavailable: its representation exceeded the 64 MiB budget. No crop implementation is inferred from that failure. InteropDependencies.dll native-boundary inventory (`ev_73a4325e2fb9ae6fffa46c610a4050f54639c26d6deea7d52fe8d5fd57810d83`) found no P/Invoke/native method declarations in that assembly; this does not imply the application lacks native code. These bounded results are retained locally, not repeatedly requested.

Native Ghidra analysis of CamtasiaFilters.dll (SHA-256 `f76b778263bbc731b4bef554bb744bc220ff34d1153c53a2d9d88d03870b224f`) completed. Defined-string regex search `crop|transform|pan` returned transform-filter/input-pin/output-pin RTTI and CompanyName, with no crop string among these matches: evidence `ev_691f1728d984ca5900c06c8c5ed48c91f368cec97a2c3db534a266314510b25d`. This bounded string result does not locate the crop implementation or prove crop is absent from the module. No proprietary pseudocode or assets were used in the independent crop implementation.

Windows live crop observation: reference at 100% view zoom, 320×180 fixture, Scale100%, Position X/Y0. Dragging left crop handle 20 displayed logical pixels right (40 source pixels on this DPI setup) changed Width320→280, kept right visible edge fixed and Position X/Y0, and did not stretch the remaining image. This directly supports source-relative clipping while retaining the original transform pivot for this unrotated case. Corners, rotation, text, animated crops, minimum size and modifiers require separate observations.
