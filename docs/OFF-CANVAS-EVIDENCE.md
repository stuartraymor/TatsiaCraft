# Off-canvas editing display: 0.0.5

## Reference observation and acceptance

In the running Camtasia26.2.2.19936 fixture at100% view zoom, moving the selected320×180 video50 logical pixels right and30 down moved its properties to X100,Y−60 source/project pixels. The original project rectangle remained fixed. The video portion beyond its right and bottom edges stayed visible but subdued, while the portion inside the project remained bright. Selection handles remained visible across the boundary. This is a qualitative visual observation, not a measured opacity or color-matching claim.

Acceptance: dim editing overflow beyond all four project edges while leaving the project interior, source data and export pixels unchanged. Keep editing grips above the shade and keep the shade from intercepting clicks. Follow project bounds during view pan, zoom, resize, detachment and project-size changes. Verify display pixels, unchanged serialized project data and exported pixels with independent tests. Exact reference shade color/alpha, HDR/video color management and every detached/fullscreen state remain open.

## Evidence boundary

44 independent checks passed: actual display pixels on all four edges and the interior; visible selection stroke above the shade; shade hit-testing disabled; view pan/zoom, rotation/scale, project-size updates and new projects; unchanged serialized data and zero document edit checkpoints for view operations. Rendering the project-sized Stage with and without the shade produced identical bytes. A decoded80×40 video export retained bright red pixels. The overlay uses75% workspace-color blending as an independent dark-theme implementation choice, not a recovered reference constant.

Reuse the existing REA artifact identities and documented UI-layer limitations in PARITY-PLAN.md. REA's native UI observer does not run on Windows, and shared managed metadata does not establish runtime compositing opacity. This increment is based on controlled Windows observations and an independent preview overlay; no proprietary style resources or artwork are copied.

The retained launcher SHA-256 was rechecked against the installed file and still matches. A current REA binary_session availability read reconfirmed `observe_native_ui` unavailable with `unsupported_host` because it requires macOS. No identical failed member analysis was repeated.

A separate 0.0.5 editor session opened a generated 320x180 split-color fixture. The right and bottom overflow appeared subdued, the project interior remained bright, and clicking the shaded right overflow selected the clip and displayed bright selection handles and its properties. Only generated test data was used.
