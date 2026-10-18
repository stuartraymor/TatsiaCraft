# Changelog

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
