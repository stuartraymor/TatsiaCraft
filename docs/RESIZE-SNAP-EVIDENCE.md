# Resize snapping: revision 0.0.7

## Acceptance before implementation

In the installed Camtasia26.2.2.19936 reference at100% view zoom, generated320x180 fixture, no crop, rotation0: dragging the right midpoint40logical pixels left gives Scale75%, Width240, X-40,Y0. Dragging it38logical pixels right (2logical pixels short of the project edge) with canvas snapping enabled gives Scale100%, Width320, X/Y0. Undo returns Scale75%. Disabling snapping with Ctrl+semicolon and repeating the identical38logical-pixel drag gives Scale98.8% (exact expected.9875), Width316, X-2,Y0. The left visible midpoint remains fixed. These are measured runtime observations for one unrotated side-handle case, not full resize parity.

Implement resize snapping under the existing canvas snapping setting/shortcut, using viewport-aware six-logical-pixel tolerance as an independent choice. Preserve the existing uniform scale, fixed opposite visible crop handle, source fractions, rotation, offset-pointer behavior and scale/position clamps. Offer project and other active visible visual layers as edge/center targets; exclude the selected/hidden/inactive/audio/animated layers. Choose the nearest permitted scale adjustment deterministically, and do not displace the dragged handle by more than the tolerance. Avoid fixed anchor coordinates as snap drivers. Show transient display-only guides for exact resulting alignment. No-op snaps must not create edit checkpoints; one gesture produces one undo checkpoint. Disabled snapping uses unchanged raw resize geometry.

Verify measured values, corner/side/rotated/cropped robustness, boundary limits, zoom thresholds, target filtering, unchanged preview/export agreement, save/reopen and UI undo. The reference guide appearance, corner/rotation/modifier behavior and exact threshold/priority remain unmeasured.

## REA boundary

Reused prior REA WPFCommonViewModel metadata/member evidence in RESIZE-EVIDENCE.md and WPFSharedFiles evidence in CANVAS-MOVE-ALIGN-EVIDENCE.md. Rechecked installed launcher SHA256ef9594e005d64ea2aee2f2ddc1d96f53cffa8c354a4c47d01a781152721537bc. Current binary_session reports no open native target; the Windows native UI observer remains unsupported. Metadata does not establish runtime snapping; no repeated failed member extraction, proprietary code or artwork is used.

## Verification

445 independent assertions passed. The measured side case matches raw Scale.9875/X-2 and snapped Scale1/X0 with the right guide at320.48 combinations of all8handles,0/90/33degree rotation and native/fitted media preserve actual WPF opposite crop anchors, crop fractions and rotation, with guide coordinates agreeing with rendered bounds. Checks cover pointer offsets, nearest/deterministic targets, fixed-anchor exclusion, upper limits, invalid values, actual compositor thresholds at50/100/200/2000% view zoom, disabled raw geometry, preview/decoded-video pixels and saved/reopened exports.

A separate0.0.7 editor session performed the38logical-pixel right-handle drag from Scale75%,X-40. Saved JSON contained Scale1,X/Y0 and unchanged zero crops. A subsequent2logical-pixel drag snapped back with no dirty marker; one undo then restored Scale.75/X-40, so the no-op added no undo step. Saving the restored generated fixture encountered one Windows replacement error, then succeeded on retry with the restored values intact. Transient replacement-lock handling remains a project-save robustness gap; no user project was involved. The reference scale/position and snapping setting were restored after observation.

Corner/rotated snapped geometry is independently robustness-tested; reference corner/rotation/modifier equivalence, exact guide styling/priority/tolerance, animated resize snapping, perspective, nonuniform scaling and multi-selection remain open. This is a bounded resize-snapping increment, not completion of the canvas workstream.
