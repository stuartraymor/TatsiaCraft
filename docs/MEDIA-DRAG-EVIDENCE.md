# Multiple Media Bin drag/drop

## Evidence boundary

Reuse the primary [TechSmith merge tutorial](https://www.techsmith.com/blog/merge-videos/) documented in MEDIA-BATCH-EVIDENCE.md: select multiple videos in Media Bin and drag them to the timeline as a sequence. REA retained partial WPFSharedFiles.dll inventory `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c` was rechecked; static member metadata does not establish drag execution. Current native capture remains unreliable; no identical failed probe is repeated and no proprietary assets/code are used.

## Acceptance before edits

- Drag one or multiple selected bin items in visible bin order to a timeline time/track, using the validated sequential insertion pipeline and one undo step. Preserve all original project clips/groups/source files. Context insertion retains its existing behavior.
- Start a drag only after Windows' horizontal or vertical minimum drag distance from a press on a real item. Clicking a selected item in a multiple selection preserves that selection for dragging; releasing without dragging selects the clicked item. Ctrl/Shift selection remains handled by WPF. Ignore background/right/released-pointer movement, busy state and stale rebuilt bins.
- Carry source project identity and current media references in the application-owned payload. Reject foreign/stale projects, missing/invalid media, invalid payloads and locked targets with no checkpoint or partial insertion. Map timeline coordinates through zoom and lane layout; clamp existing outer-bound coordinates, reject nonfinite positions.
- Tests exercise real WPF event wiring and data objects, threshold/deferred-click state, drop handler integration, ordering, nonzero time/track placement, stale/missing/busy/lock/cancel cases, undo/redo/save, preview and decoded export. A source-owned drag executor seam permits testing without entering the operating-system drag loop.
- Run all release gates, install and rerun installed workflow, then publish and verify commit/tag/source/executable identities.

## Independent policies and remaining checks

Visible order, overlap behavior, root-timeline insertion, native-size clips, coordinate clamping, application-local payload and deferred selection policy are independent choices. Exact current reference modifier/gesture/scroll/snapping behavior, cross-process media drags and physical native drag-loop observations remain open. Source-owned WPF event/handler tests do not establish physical mouse or Camtasia runtime equivalence.

## Focused validation

40 added checks pass (MediaBinTest270). Own-source tests raise WPF press/release and Drop events, drive the same movement delegate with explicit pointer state, and substitute the drag executor to avoid the OS loop. They cover thresholds, deferred selection, visible-order immutable arrays, one drag per press/cancel/released/busy/rebuilt-bin behavior, project identity after undo, stale/malformed/unrelated/missing data, locked drop/cursor feedback, nonzero time/lane placement and outer coordinate clamping. Actual preview and decoded export, save/reopen, one-step undo/redo and original source bytes are verified.

Harness repairs corrected C# variable scope, target-lane setup and synthetic event routing/hit-testing of unrealized ListBoxItems. A later existing cleanup-undo assertion failed once; diagnostic rerun passed without a production change. Its cause is unresolved; the full release gate rechecks it. Physical drag-loop interaction remains pending.
