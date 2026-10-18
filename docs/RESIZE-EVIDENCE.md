# Cropped-media resize acceptance: revision 0.0.4

Reference: installed Camtasia 26.2.2.19936, existing generated 320×180 video fixture. Experiments were made in the existing editor at100% view zoom, unrotated, source Scale100%, X/Y0. Reference interactions are visual/runtime observations; static metadata is recorded separately below.

## Measured cases and acceptance criteria

1. Crop40 source pixels from the left (12.5%). Visible width becomes280; X/Y remain0. In Select mode, drag the bottom-right visible handle70×45 logical display pixels (140×90 source/project pixels at this DPI). Camtasia reports Scale150%, X60, Y−45 and visible Width420. The visible top-left crop corner stays fixed. Accept matching scale/position, fixed opposite visible corner, unchanged crop fractions and proportional dimensions.
2. Undo only that resize, retaining the left crop. Drag the right middle handle40 logical pixels right (80 project pixels). Camtasia reports Scale128.6%, X34.3, Y0 and visible Width360. Its opposite visible left midpoint stays fixed while height grows proportionally. Accept the exact calculated values9/7 scale and240/7 source X within the reference's displayed rounding.
3. A resize must be a single completed edit for undo; save/reopen and exported pixels must retain the resulting crop/scale/position. Source media bytes must remain unchanged.

The implementation snapshots the explicit visible handle and its opposite, rather than reflecting the mouse position about the full uncropped source center. Mouse motion is relative to the pointer-down offset, preventing a grip click away from its center from changing the initial geometry. Scale and position limits constrain the same anchored transform rather than separately moving its anchor.

## Coverage boundaries

Validation:480 independent assertions passed, including48 combinations of handle/rotation/native-versus-fit layout, rendered WPF anchors and decoded export pixels. The actual editor mouse workflow reproduced Scale150%, X60 and displayed Y−45 after the measured cropped-corner drag; saved JSON retained Scale1.5, X60, internal Y45 and CropLeft0.125. One Ctrl+Z restored Scale1 and X/Y0 while retaining CropLeft0.125, then saving persisted that restored state.

All eight handle directions, rotated crops, fit-to-canvas versus native sizing, off-center grip clicks, numeric limits, invalid inputs, preview geometry and export pixels receive independent tests. Only the two unrotated reference gestures above were directly measured in this pass. Rotated and modifier-key parity must not be inferred from geometry tests alone. Animated starting-transform edits, perspective transforms, nonuniform stretch and source motion inside a crop remain outside this increment.

## REA evidence

`inspect_managed_artifact` on WPFCommonViewModel.dll returned `ev_0f7b5480fe7fb2fbcab65bbdfb557d240cc14f558483c643e070d3a86157a378`, complete metadata inventory:40 TypeDef rows and136 MethodDef rows. `inspect_managed_members` returned `ev_4783a31bb509b997abadd832d9487f1f910b7556f2d2bae7ff5dca9aea3e40e9`, coverage explicitly partial. Its returned types include shared ActionCommand, RelayCommand, AsyncCommand and property-notification infrastructure; no returned type identifies crop/resize geometry. These are bounded static observations, not a recovered resize implementation. The independent implementation is based on the live measurements above; no proprietary code or artwork was copied.
