# Media Bin source preview increment

## Evidence and boundary

TechSmith's [Camtasia Studio 7 Help](https://assets.techsmith.com/docs/pdf-camtasiastudio/camtasia_studio_7_online_help.pdf) documents double-clicking a Clip Bin item to preview it. The [Windows 2020 history](https://support.techsmith.com/hc/en-us/articles/35532296446733-Camtasia-Windows-2020-Version-History) confirms a separate Media Bin preview capable of playing alongside the canvas. These establish a historical source-preview workflow, not the exact 26.2.2 dialog or gestures.

REA retained evidence `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c`, WPFSharedFiles.dll SHA-256 `46b37ce47761c253e8106cca0e1a28e275aaefbe7b93117b8cfd3ba2e0991822`, was rechecked. Its static member coverage does not establish Media Bin preview execution. Prior Windows native observations remain unavailable/unreliable; no repeat of identical failed capture/analysis and no runtime equivalence claim.

## Acceptance defined before implementation

- Double-click a single real bin item to preview its original source without inserting a timeline clip. A Preview context command exposes the same action; explicit Add to Timeline at Playhead retains insertion access alongside drag/drop.
- Preview an independent snapshot with its own live decoder, fit view, play/pause, stop and seek controls, position/duration and visible decoder errors. Preview canvas cannot edit source transforms. Close/Escape disposes playback and decoder resources.
- Reject empty/multiple/stale selections, busy operations, missing sources and invalid metadata without modifying project state or opening a dialog. Decoder failures remain visible rather than implying success.
- Preview a fixture image, audio and video; exercise actual WPF controls and double-click/context handlers. Verify playback advances, pause/stop/seek boundaries, disposal and snapshot independence.
- Project JSON, head, timeline selection, dirty/undo/redo, saved/reopened state, source bytes and main preview/export remain unchanged by preview. Explicit insertion remains one undoable action and obeys existing busy/track guards.
- Run focused checks, complete release gates, install and rerun the workflow against the exact installed executable before publication.

## Independent choices and gaps

An owned modal preview window, starting paused, and the explicit context commands are independent interaction choices. Images use their imported duration. Audio uses a neutral black canvas with a type label. Source dimensions up to the existing model limit are retained; preview is display-only. No reference assets or code are copied. Exact current reference layout, autoplay policy, keyboard shortcuts, volume/full-screen controls, multiple simultaneous previews and codec compatibility remain unmeasured. Audible playback quality and physical native gestures require interactive verification; source-owned WPF tests are not a Camtasia runtime observation.

## Focused validation

78 additional checks pass (MediaBinTest153). Tests open real WPF dialogs through Preview and the bin double-click handler, render original image pixels, await audio/video MediaOpened through NaturalDuration (including 320x180 video dimensions), advance actual dispatcher playback, exercise seek/play/pause/stop/close, verify resource release and visible corrupt-image errors, and compare original project/undo/redo/head/selection, saved/reopened data, source bytes and decoded main export. Explicit context insertion is busy/locked guarded and one-step undoable. Earlier test repairs corrected a direct-routed synthetic event and used the bundled MPEG-4 encoder after the fixture encoder libx264 was unavailable; no production behavior was inferred from those harness failures.
