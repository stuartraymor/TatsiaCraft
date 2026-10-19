# TatsiaCraft

An independent Windows video editor written in C# and WPF, with an external FFmpeg media pipeline. The existing application's executable, namespace and project format currently retain the name **TasiaCraft**.

This repository contains original application source and tests under the MIT license. It is a development preview with partial feature coverage. Complete Camtasia parity, identical artwork and layout, and comparative performance improvements have **not** been established. This project is not affiliated with TechSmith.

## Revisions

Current version: see [VERSION](VERSION). The top-left title bar, Home and About display that same version. Revisions run from 0.0.1 through 0.0.12, then 0.1.0; minor also rolls over after 12. Every completed revision must be uploaded to GitHub and published as a release. Developers must follow [CONTRIBUTING.md](CONTRIBUTING.md) and [AGENTS.md](AGENTS.md), using `scripts/release.ps1` to validate and publish.

## Build

Requirements: x64 Windows, .NET Framework 4.8 desktop runtime and its Framework compiler. No Visual Studio or .NET SDK is needed for the existing build script.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1
```

Output: `dist/TasiaCraft/TasiaCraft.exe`.

For media import, recording, effects and export, place compatible Windows FFmpeg and FFprobe executables and their required DLLs in `dist/TasiaCraft/tools/`. The current media pipeline requires the `libopenh264` encoder, AAC support and the filters used in `src/Engine.cs` and `src/LivePreview.cs`. FFmpeg and its dependencies are separately licensed; include their required licenses and corresponding source when distributing them. Third-party binaries and dependency source archives are not included in this source repository. Dependency redistribution and reproducibility require further validation.

## Current functionality

- Multitrack editing, move/trim/split, copy/paste, undo/redo and JSON projects.
- Live composition and playback with source effect proxies.
- Bidirectional Media Bin/timeline source selection, including group units and separated streams.
- Media Bin source preview by double-click/context, with independent playback, seeking and decoder cleanup; explicit context insertion and drag/drop.
- Canvas movement, anchored resizing, rotation and independent view zoom up to 2000%.
- Image/video cropping with live handles and numeric properties, retained source placement and transparent export.
- Cropped-media corner/edge resizing anchored to the opposite visible crop handle.
- Hand-mode viewport panning with independent clip geometry and Fit recentering.
- Subdued off-canvas editing overflow with bright selection grips and unchanged export pixels.
- Detachable canvas with retained transport, zoom, selection and playhead.
- Basic text, shapes, transitions, visual/audio presets and SRT import.
- Desktop, microphone and webcam capture, and MP4 export through FFmpeg.

Coverage is limited. Advanced keyframes, ripple/group/multiple selection editing, full effects/cursor catalogs, native Camtasia formats, transcription, AI/cloud workflows and many export options remain unfinished. Decoder support and synchronization depend on Windows media components. No sample-accurate playback or exhaustive format support is claimed.

## Tests

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-canvas.ps1
```

This builds and runs the isolated canvas workspace harness on an interactive Windows desktop. Its 28 checks exercise versioned title states, all zoom levels, detaching/reattaching, playback, shutdown and unchanged serialized project geometry. Results are written under `artifacts/`.

`tests/LivePreviewTest.cs` is an additional integration harness requiring FFmpeg/FFprobe and generated image/video fixtures under an explicitly supplied test root. It is retained for development and is not invoked by the canvas script.

## Source map

`App.cs` coordinates editing and project operations; `EditorUI.cs`, `EditorTheme.cs`, `EditorTimeline.cs`, `EditorHome.cs` and `PaletteUI.cs` build the interface; `LivePreview.cs` implements the live compositor; `Engine.cs` handles project validation and media processing.

Research captures, proprietary assets, installers, media files, local account data and investigation reports are excluded.

## Feature plan and installation

The [parity plan](docs/PARITY-PLAN.md) tracks each workflow, evidence boundaries and remaining gaps. [Installation instructions](docs/INSTALLATION.md) explain versioned per-user installs and payload validation. For the crop acceptance suite, run `scripts/test-crop.ps1 -ToolsPath <compatible-tools-directory>`; it checks actual preview and decoded export pixels, save/legacy projects and invalid input.

`scripts/test-pan.ps1` runs 33 viewport, tool-mode and state-preservation checks without external media tools.

`scripts/test-resize.ps1 -ToolsPath <compatible-tools-directory>` checks anchored resize geometry against actual WPF crop bounds, reference measurements, preview/export pixels and persisted projects. See [resize evidence](docs/RESIZE-EVIDENCE.md) for the exact observed behavior and remaining limits.

`scripts/test-offcanvas.ps1 -ToolsPath <compatible-tools-directory>` checks display shading, viewport changes, selection visibility and undimmed frame/video exports. Its reference and coverage boundaries are in [off-canvas evidence](docs/OFF-CANVAS-EVIDENCE.md).

Canvas crop mode supports dragging source media beneath a fixed crop frame. Canvas selection supports edge/center snapping and transient alignment guides; use View > Enable Canvas Snapping or Ctrl+semicolon. Modify > Align to Canvas provides six explicit alignment actions. These convenience command placements and gold guide styling are independent implementation choices. See [canvas evidence](docs/CANVAS-MOVE-ALIGN-EVIDENCE.md).

Focused checks: `scripts/test-cropmove.ps1 -ToolsPath <tools>` and `scripts/test-alignment.ps1 -ToolsPath <tools>`.

Resize handles also use canvas snapping without moving the opposite visible crop handle. Disable View > Enable Canvas Snapping for raw resizing. Focused check: `scripts/test-resizesnap.ps1 -ToolsPath <tools>`. See [resize snapping evidence](docs/RESIZE-SNAP-EVIDENCE.md).

Project saves retry short-lived Windows sharing and replacement-lock failures within a bounded window. Persistent failures preserve the previous destination; failed Save As keeps the active project path and unsaved state. Focused check: `scripts/test-projectsave.ps1 -ToolsPath <tools>`. See [save evidence and limitations](docs/PROJECT-SAVE-EVIDENCE.md).
