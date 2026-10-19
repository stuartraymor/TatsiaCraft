# Source preview failure and retry

## Defect and evidence boundary

Our source-preview player reports decode errors through the same status channel as ordinary playback. Its Play handler remains enabled and calls LivePreview.Play, which reports Playing after a corrupt image has already failed. Reproduce this against the previous released assembly with a corrupt image and actual WPF buttons before repairing it.

This is an independent product defect repair, not a claim about Camtasia internals or its error policy. Historical source-preview workflow evidence remains in MEDIA-PREVIEW-EVIDENCE.md. Retained REA partial WPFSharedFiles.dll member evidence `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c` was rechecked; it cannot establish runtime failure or retry semantics. Current native reference observation remains unavailable, and identical failed probes are not repeated.

## Acceptance before edits

- Expose typed live-preview errors separately from ordinary statuses for image, decoder-open, asynchronous decoder and proxy failures. Keep existing status subscribers compatible.
- On source-preview failure, pause its playback, persist the error text, disable Play/Stop/Seek and enable Retry. Attempts to invoke a disabled transport handler cannot resume playback or overwrite the error. Close/Escape remains usable.
- Retry closes old layers/decoders, resets source position, and reopens the same independent source snapshot. An unchanged broken source remains failed; a repaired source renders successfully and re-enables transport. A source removed after opening reports an honest failure without modifying the project.
- Verify corrupt image and asynchronous video failure, repeated failed retry, repaired image retry, disabled commands, cleanup, source bytes, project dirty/undo/head/selection/save and main preview/export preservation.
- Run full gates, install and rerun against the exact installed executable, then publish and verify remote source/commit/tag/asset identity. Exact reference retry/error wording and layout remain open.

## Reproduction and focused result

Before the repair, the real WPF corrupt-image dialog reported `Image preview failed: No imaging component suitable to complete this operation was found.` Clicking Play then reported `Playing live timeline` with IsPlaying=true. The expected failure-latch assertion failed against the prior source; baseline JSON and failed logs are retained in ignored local artifacts.

First repair testing exposed a source-file sharing violation when replacing the corrupt image. Loading BitmapImage from a FileStream inside a using block (OnLoad) now closes the stream on success or exception, permits source repair, and avoids URI-cache reuse. No source-file bytes are changed by the player; fixture repairs are explicit test actions.

24 added checks pass (MediaBinTest294): persistent errors; disabled Play/Stop/Seek handlers; Retry on unchanged broken source; recovery from red fixture; file removal and second recovery to fresh blue pixels; asynchronous video error; close/disposal; untouched editor JSON/head/selection/undo/dirty and main preview/decoded export/save. Existing image/crop/resize/export gates cover the shared image-loading change. Exact reference error/retry behavior and physical keyboard/mouse observation remain pending.
