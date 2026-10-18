# Interface wording, menus and icons (0.0.11)

## Reference observation

Target: installed Camtasia 26.2.2.19936 editor, observed live on Windows on 2026-10-18 with an untitled project and one fixture clip.

- **Method.** I read the Windows UI Automation tree of the editor window: 1,478 nodes, giving names, automation IDs, help text and bounds. I expanded each top-level menu through the ExpandCollapse pattern and closed it without invoking anything. A screenshot at 1389×812 logical pixels recorded placement and visual style.
- **REA.** `observe_native_ui` is bound to Mach-O targets, so it does not apply to this Windows target. No REA evidence is claimed for this increment.
- **What was used.** Only labels, tooltip text, menu structure, ordering and layout were taken from the reference. No reference code, images, icon files or other assets were extracted, decompiled or copied. Every TatsiaCraft icon is an original vector drawing in `EditorTheme.ToolPicture`.

Observed reference wording reproduced in this increment:

- **Menus:** File, Edit, Modify, View, Export, Help.
- **File:** New Project, New Project from Template..., Open Project, Recent Projects, Home, Save, Save As..., Import > Media... / Captions..., Project Settings..., New Recording..., Exit.
- **Edit:** Undo, Redo, Cut, Copy, Paste, Duplicate, Delete, Split Selected, Group, Ungroup, Select > Select All, Deselect All, Jump To > Beginning / End / Next Media / Previous Media.
- **View:** Tools > (tool list), Hide Properties, Canvas > Zoom In / Zoom Out / Zoom to 100% / Detach Canvas, Timeline > Zoom In / Zoom Out / Zoom to Fit, Enable Canvas Snapping.
- **Export:** Local file..., Export Frame As....
- **Help:** Quick Start Guide, Keyboard Shortcuts.
- **Toolbar tooltips:** Open the Home Window (Ctrl+H); Record the Screen (Ctrl+R); Edit; Pan (Hold Spacebar); Crop (Hold Alt); Canvas Options (Ctrl+= / Ctrl+-); Export and Share.
- **Transport:** Previous frame (,); Next frame (.); Play (Spacebar); Previous media (Ctrl+Alt+,); Next media (Ctrl+Alt+.); clock `00:01 / 00:04`; `30 fps` and `(1.0x)` with the tooltip "Current Playback Speed"; Properties (Ctrl+2) as a green button with a gear icon.
- **Timeline toolbar:** Undo (Ctrl+Z), Redo (Ctrl+Y), Cut (Ctrl+X), Copy (Ctrl+C), Paste (Ctrl+V), Split (S); "Save an image of the current video frame on the Canvas. (Ctrl+Shift+F)"; "Bring all media on timeline into view (Ctrl+Shift+7)"; "Zoom timeline out (Ctrl+Shift+-)"; "Zoom timeline in (Ctrl+Shift+=)"; "Add a track" at the top left of the track headers.
- **Timecode:** ruler and playhead use `h:mm:ss;ff`, for example `0:00:01;25`.
- **Window caption:** `Camtasia Editor - Untitled Project*`.
- **Media Bin:** title "Media Bin"; import tooltip "Import Media (Ctrl+I)".
- **Tool list order:** Favorites, Media, Annotations, Visual Effects, Transitions, Animations, Behaviors, Cursor Effects, Audio Effects, Narration, Captions. The active tool is marked by an accent bar on its right edge.

## Independent choices and limits

- **Shipped only where it works.** Menu items, tooltips and shortcuts ship only when TatsiaCraft has the behavior behind them. The reference entries that are not shown include Favorites, Behaviors, Cursor Effects, Narration, Corner Pin, markers, quizzes, Ripple Delete, Stitch, Copy/Paste Effects, Library export, batch export, Screencast/YouTube and Edit as Text. Adding them is future feature work, not wording work.
- **Two labels drop their hold-key hints.** Pan and Crop omit "(Hold Spacebar)" and "(Hold Alt)" because temporary hold-key tool switching is not implemented.
- **Caption.** The caption keeps the required `TatsiaCraft <version>` prefix: `TatsiaCraft 0.0.11 - Untitled Project*`. Saved projects show the project file name.
- **Kept TatsiaCraft items.** Library stays a separate left-navigation item; the reference shows it as a Media Bin tab. Other TatsiaCraft-only entries stay, with reference-style capitalization: Open group/Rename Group/Main Timeline, Align to Canvas, Reset Transform, Add Animation > Scale Up/Pan Right, Cancel Render, Relink Media....
- **Properties placeholder.** The empty-selection Properties text is not reproduced; the panel still collapses when nothing is selected.
- **Not yet measured.** Icon shapes, colors, sizes and spacing are approximate. No pixel-level visual comparison is claimed.

## Acceptance

`scripts/test-ui.ps1` runs 75 checks:

- caption format, including clean and dirty states;
- the order of every top-level menu and of the File, Import, Edit, Jump To, View Tools, View Timeline, Export and Help menus, plus Edit shortcut text;
- three-dot ellipsis use;
- left navigation order, and that renamed palettes still populate;
- 24 tooltips;
- that the duplicate Project Settings toolbar button is gone, and that vector icons are present;
- transport clock and frame-rate formats, and `h:mm:ss;ff` timeline time with hours;
- `,`/`.`/Home/End keys, previous/next media edges, the Properties toggle, timeline zoom with slider sync and clamp, zoom to fit on a 60 s timeline, canvas zoom steps, and undoable Add a track.

It also renders `artifacts/UiChromeTest/editor-1389x812.png` for comparison with the reference screenshot.

Ctrl- and Alt-modified shortcuts are covered by calling the commands they invoke, not by synthesized modifier key state.
