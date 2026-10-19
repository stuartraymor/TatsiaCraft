# Changelog

## 0.1.9

- Extend Add to Timeline at Playhead to multiple selected Media Bin entries, inserting a consecutive sequence in visible bin order in one undo step. Select the resulting clips on Main Timeline and retain existing overlaps and groups.
- Validate the complete batch before changing the project: reject missing/stale sources, invalid metadata, locked targets and duration overflow. Preserve existing project data and sources; configure an empty unconfigured canvas from the first visual source.
- Add 39 checks (230 media-bin checks), including actual context insertion, sorting, atomic failures, undo/redo, save/reopen and preview/decoded export agreement. Multi-item dragging and exact reference ordering/collision policies remain open.

## 0.1.8

- Add Media Bin Select on Timeline and timeline Select in Media Bin context commands. Find every selected source reference, keep root groups together, and reveal distant timeline results without seeking. Reverse navigation reveals source entries through type filters while retaining sort/layout.
- Keep missing-file references navigable, distinguish aliases by media ID, support separated streams and multiple selections, and guard empty/unused/stale/busy cases. Navigation preserves project data, playback, head, source files and undo/redo.
- Add 38 focused checks (191 media-bin checks), including actual context handlers, distant scrolling, hidden/locked/grouped clips, preview/export and save/reopen preservation. Exact current reference selection, focus and scrolling policies remain unmeasured.

## 0.1.7

- Preview a single Media Bin source through double-click or Preview, with an independent fit player, play/pause, stop, seek and position/duration. Previewing no longer inserts a timeline clip. Use Add to Timeline at Playhead or drag/drop to insert.
- Clone source metadata for preview; disable canvas edits, show missing/invalid/decoder errors, guard busy and stale/multiple selection, and release decoders on close. Keep timeline state, project data, undo and export unchanged.
- Add 78 focused checks (153 media-bin checks), including actual WPF context/double-click handlers, image pixels, opened audio/video decoders, transport controls, error reporting, cleanup and preview/export/save preservation. Current Camtasia dialog/layout/autoplay parity and audible quality remain unmeasured.

## 0.1.6

- Add an Ascending/Descending button beside the Media Bin Name/Type sort control, with stable case-insensitive ordering and filter support.
- Keep selected visible media through sorting, layout changes, thumbnail refreshes and tool switches. Clear hidden selections when filtering and clear old selection when opening another project, keeping Relink/Details targets accurate.
- Keep view changes out of project data, undo history and exported output. Add 29 focused checks (75 media-bin checks total), including actual buttons, selection retention, Unicode/ties, save/reopen and preview/decoded export preservation. Exact current reference collation and layout remain open.


## 0.1.5

- Add Media Bin context commands Details, Select Unused Media and Delete Unused Media. Select Unused reveals all media and selects entries absent from every timeline clip, including grouped/hidden/locked and separated-stream references.
- Delete unused project references in one undo step, with redo/save/reopen; retain source files and still-used entries sharing the same file.
- Show read-only stored media details and file availability. Require a single bin entry for Relink, guard unsupported multiple-item insertion/dragging, and distinguish empty filters from an empty project.
- Scope Ctrl+A/deselect to a focused bin, and guard unsupported bin editing shortcuts so Delete/Cut cannot affect selected timeline clips.
- Add 46 checks covering selection/dirty/undo state, source-byte preservation, filters, missing media, actual Details dialog and unchanged preview/decoded export. Exact current reference dialog/layout and batch insertion remain open.


## 0.1.4

- Add the Favorites tool. A star on every preset tile adds it to Favorites, with a Type filter and the reference empty state. F opens it. Favorites are saved per user.
- Add the Behaviors tool and Modify > Add Behavior with 8 behaviors: Drifting, Fade, Fly In, Pop Up, Pulsating, Scale, Sliding and Tremble. Each has In and Out durations and an on/off toggle in Properties, and preview and export render them identically for media and text. Navigation order now matches the reference, with Favorites first and Behaviors after Animations.
- Add 80 behavior and favorites checks, with export/preview pixel probes. Explode, Jump And Fall, Reveal, Shifting, Wiggle and the Style/Movement options remain open.

## 0.1.3

- Recover export publication from short-lived Windows sharing/lock errors with the existing bounded atomic commit policy: five attempts, up to 1.5 seconds of delay, only HRESULT32/33/1175.
- Keep the previous output intact on persistent failures and clean the temporary MP4. Show Finalizing export before commit and report completion only after success. Project saves reuse the same helper.
- Build releases from the staged C# source list and reject tracked/index changes during verification, keeping unrelated untracked work out of published executables.
- Add 24 checks using real temporary/permanent no-delete locks, read-only/missing destinations, original-byte/source protection, temporary cleanup and decoded picture/audio/duration. This repair does not establish reference export internals or network-filesystem/crash recovery parity.


## 0.1.2

- Add audio points (volume envelopes) on audio clips. Double-click the green level line to add a point, drag points to change time and level, right-click to delete, and use Modify > Remove > All Audio Points to clear them. Points follow the source audio through trims, splits, stitches, ripple inserts and speed changes, and apply in preview and export.
- Add a time range from the playhead's green start and red end handles. Silence Audio (Shift+S) with a range silences just that span with short ramps, on the selected clips or on every overlapping audio clip. Restore Audio (Shift+R) with a range removes those points. Without a range, Restore also clears a clip's audio points.
- Fix: exports of audio clips with both a source offset and a later timeline start produced a corrupt audio stream. Mixed audio is now retimed correctly.
- Add 69 checks with time-windowed loudness measured on decoded exports. Exact reference point gestures and scale remain unmeasured.

## 0.1.1

- Silence Audio (Shift+S) and Restore Audio (Shift+R) from the clip context menu remove and restore a clip's audio in preview and export without changing its gain. Silenced clips are labeled on the timeline.
- Stitch Selected Media (Ctrl+Alt+I, Edit menu, context menu) rejoins selected clips that touch and continue the same media with matching settings, restoring the original clip exactly.
- Ripple Insert: Shift-drag the playhead to insert blank time on every unlocked track. Clips crossing the playhead split, and later clips and markers move right. It is one undo step, and inserting past the 24-hour limit is rejected.
- Fix: Edit menu now orders Split Selected, Stitch Selected Media, Separate Audio and Video as in the reference.
- Add 68 checks with decoded audio and picture comparisons. Audio-point envelopes, range-based silencing and stitching non-continuous media (as in the reference) remain open.

## 0.1.0

- Ripple Delete (Ctrl+Backspace, Edit menu, clip context menu) removes the selected clips and closes their gap on each affected track. Other tracks and markers stay put, and it is one undo step.
- Ripple move: Shift-drag a clip to move it together with every later clip on its track. Grouped followers move with it. Shift-click without dragging still toggles the selection.
- Separate Audio and Video (Edit menu, clip context menu) splits a video clip that has audio into a video-only clip and an audio-only clip, placed on a new track directly above, as in the reference. Export and live preview play each half on its own, so it can be hidden, moved or muted.
- Add 76 ripple and separation checks covering decoded audio and picture, preview, save/reopen, locked tracks, groups and undo. Silence/Restore Audio, Stitch and Ripple Insert remain open.

## 0.0.12

- Add timeline markers, following the observed reference workflow. Ctrl+M or View > Show Markers shows the Marker tray. Add a marker with Shift+M, Modify > Markers > Add Timeline Marker, or a click on the tray. New markers are named Marker N.
- Rename markers by double-clicking, from the right-click menu, or in the Marker name field of the new Marker properties panel. Delete them from the right-click menu, the Delete key or Remove All Markers. Each edit is one undo step, and markers are saved with the project.
- Jump to the next or previous marker with Ctrl+] and Ctrl+[. Beginning and End are now Ctrl+Home and Ctrl+End, and Deselect All is Ctrl+Shift+D. Markers snap to clip edges, other markers and the playhead, clip and playhead drags snap to markers, and Shift turns snapping off.
- Exported MP4 files contain one chapter per marker. A marker after the last clip extends the timeline and the export.
- Fix Unicode decoding of FFmpeg/FFprobe output.
- Add 98 marker checks covering the model, decoded chapter export, the editor and menus. The marker thumbnail, quizzes and the transcript tray, the Markers snapping toggle and the Smart Player table of contents remain open.

## 0.0.11

- Reorganize menus to the observed reference structure and wording: File (Recent Projects, Import submenu), Edit (Split Selected, Select, Deselect All, Jump To), Modify, View (Tools, Canvas and Timeline submenus, Hide/Show Properties), Export (Local File..., Export Frame As...) and Help (Quick Start Guide, Keyboard Shortcuts), with shortcut text.
- Replace text glyphs with original vector icons for Home, Record, Export, left tool navigation, transport, Properties and timeline zoom. Use reference tooltips, a pill-shaped Record button, round transport buttons, green Properties/Export buttons and an accent bar on the active tool.
- Use the window caption `TatsiaCraft <version> - Untitled Project*`, an `mm:ss` transport clock with frame rate and speed, and `h:mm:ss;ff` timeline time. Move Add a track to the track header corner and remove the duplicate toolbar Project Settings button.
- Add working shortcuts and commands: , and . for frame step, Home/End, Ctrl+Alt+, and Ctrl+Alt+. for previous/next media, Ctrl+2 for Properties, Ctrl+= and Ctrl+- for canvas zoom, Ctrl+Shift+= and Ctrl+Shift+- for timeline zoom, Ctrl+Shift+7 for zoom to fit, and Ctrl+Shift+F for frame export. Keep the timeline zoom slider in sync.
- Add 75 UI wording, menu, tooltip, icon and shortcut checks. Reference-only tools (Favorites, Behaviors, Cursor Effects, Narration, markers, quizzes and others) are not shown until they are implemented. Pixel-level icon parity is not claimed.

## 0.0.10

- Rename complete groups from properties, Edit and timeline context menus, with an owned Rename/Cancel dialog and visible validation errors.
- Support trimmed Unicode names up to 200 characters. Protect locked members and ambiguous selections; unchanged, cancelled and invalid names leave undo and dirty state untouched.
- Preserve group/member identities, timing, geometry and output, with one-step undo/redo and saved names. Clip long labels within timeline blocks and retain the full name in the tooltip.
- Add 37 naming checks (223 grouping checks total), including exact preview/decoded export and save/reopen. Exact reference dialog behavior and nested groups remain open.


## 0.0.9

- Select multiple clips with Ctrl/Shift-click or Ctrl+A; group with Ctrl+G and ungroup with Ctrl+U. Display collapsed group blocks with Open group and Main Timeline member editing.
- Move selected clips together on the canvas and timeline, preserving spacing, timing gaps, crop geometry and animation endpoints. Keep selection outlines out of exported video.
- Copy, cut, paste, duplicate and delete whole selections/groups, with fresh identities, saved group membership, atomic locked-track guards and one undo checkpoint per edit. Preserve old project loading.
- Add 186 focused grouping/selection/clipboard/movement, preview/export, save/reopen and editor regression checks.
- This first increment supports flat groups and absolute member timing. Nested groups, independent group timelines, group transform/effect controls and complete reference interaction/layout parity remain open.


## 0.0.8

- Recover automatically from short-lived Windows sharing/lock errors while committing a project save. Retry only three known HRESULTs, with five attempts and at most 1.5 seconds of retry delay; persistent and unrelated errors still report failure.
- Keep validated same-directory temporary writes and atomic replacement. Never truncate or delete the previous project as a fallback.
- Commit Save As path, dirty state and versioned title only after saving succeeds, preserving the previous destination and edits on failure.
- Add 60 regression checks for real temporary/permanent locks, original-byte preservation, temporary-file cleanup, Unicode paths, source protection, Save As state and decoded export after recovery.
- This is an independent robustness repair; crash/power-loss recovery, network filesystems and reference save internals remain unverified.

## 0.0.7

- Apply canvas snapping to resize handles while retaining uniform scale, the opposite visible crop anchor and source crop/rotation values.
- Reuse the canvas snapping toggle, active-layer targets, zoom-aware tolerance and display-only guides. Disabled snapping keeps the existing raw geometry; clamped/no-op resizes avoid extra checkpoints.
- Add445 independent resize-snap checks covering the measured reference case, every handle with cropped/rotated/native/fitted media, pointer offsets, deterministic targets, limits, actual viewport zoom, preview/export and save/reopen.
- Exact reference corner/rotation/modifier snapping, guide styling/priority and full animated editing remain unverified.

## 0.0.6

- Drag source media inside a fixed crop aperture, with source-edge clamps and one gesture per undo checkpoint.
- Snap visible crop edges and centers to the canvas and active visual layers; show transient guides outside frame/video export. View > Enable Canvas Snapping and Ctrl+semicolon toggle snapping.
- Add six independently placed Modify > Align to Canvas commands, with visible cropped/rotated bounds and undo/redo. Hidden, locked, inactive, audio and animated selections are guarded.
- Add 286 crop-movement and 68 alignment checks including actual preview/export pixels, save/reopen, zoom-aware tolerance, guide rendering/export exclusion and editor undo/redo.
- Add a release preparation/install step so the installed workflow can be checked before publication, reusing the exact verified executable.
- Exact reference guide styling, tolerance, resize snapping, multi-selection and animated crop interaction remain unverified.

## 0.0.5

- Subdue media extending beyond the project rectangle during canvas editing while leaving the project interior bright.
- Keep selection grips above the shade and preserve click-through editing of overflow.
- Follow viewport pan/zoom, workspace resize and project-size changes without changing document or source data.
- Verify that project-sized frame rendering and exported video pixels remain undimmed.
- Add44 independent display, state-preservation and export checks to the release gate.

The shade color/opacity is an independent dark-theme design choice; exact reference alpha, HDR/color management and complete display-style parity remain unverified.

## 0.0.4

- Correct cropped-media resizing to anchor the opposite visible crop corner or edge midpoint instead of the uncropped source bounds.
- Preserve proportional sizing, source rotation and crop fractions, including off-center grip clicks.
- Constrain scale and position while retaining the anchor; avoid new undo checkpoints for scale gestures that do not change the transform.
- Add independent geometry, actual preview, decoded export and persistence acceptance checks, including the two measured reference gestures.
- Include the resize suite in the mandatory test/install/publish release gate.

Rotated geometry is independently tested; only unrotated bottom-right and right-middle reference gestures were directly measured in this revision. Modifier-key, animated and perspective resize parity remains open.

## 0.0.3

- Add a canvas hand tool that pans the workspace without changing clip properties, export geometry, dirty state or undo history.
- Hide transform grips while panning and make selection, crop and pan modes mutually exclusive.
- Recenter the viewport when choosing Fit, including when Fit is already selected.
- Retain pan across source refresh, magnification changes and canvas detachment; reset it for a different project.
- Add 33 viewport/mode/state acceptance checks and include them in the install-and-publish release gate.

Actual mouse testing matched the measured 50×30 view displacement with unchanged Scale100% and Position X/Y0; Fit returned to center. Pan remains session-local. Saved viewport state, temporary pan shortcuts, wheel anchors, reference viewport constraints and exact hand-icon artwork remain unverified.

## 0.0.2

- Add an evidence-driven feature/workflow parity plan and explicit remaining gaps.
- Add canvas Crop mode with edge/corner handles, numeric crop properties, reset and undo. Cropping retains source placement, scale and transform pivot.
- Preserve crop through project save/reopen, preview composition and transparent video/image export; reject invalid crops and retain legacy uncropped defaults.
- Add 37 crop/preview/export acceptance checks and an actual mouse-drag/save/undo workflow check.
- Add immutable per-user versioned installation, payload hash manifests and isolated installation tests.
- Require crop, canvas, installation and installed startup/shutdown verification before GitHub publication. Installed and published executable hashes must agree.

Known limits: crop UI and icons are independently drawn and differ from the reference; cropped-object resize anchoring, moving source within the crop region, text crop and dynamic crop animation are not established as equivalent. Full application parity remains unfinished.

## 0.0.1

- Begin the requested revision sequence: 0.0.1 through 0.0.12, then 0.1.0.
- Show the product version beside the icon/name in the top-left title bar, including new, open, saved and unsaved project states.
- Use the same version on Home and About.
- Generate assembly and file versions from the root VERSION file.
- Document mandatory GitHub publication for every completed revision in code, AGENTS.md and CONTRIBUTING.md.
- Add a validated commit/tag/push/release script with executable and source assets and interrupted-release recovery.

This revision starts a new release sequence; previous development builds used a different informal version number. It remains a development preview.
