# Batch insertion at the playhead

## Evidence and scope

TechSmith's [merge videos tutorial](https://www.techsmith.com/blog/merge-videos/) documents selecting both videos in the Media Bin, dragging them to the timeline, and arranging the resulting sequence. This supports sequential batch insertion as a workflow; it does not establish exact current ordering, collision policy, or context-command behavior.

Retained REA WPFSharedFiles.dll evidence `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c` (SHA-256 `46b37ce47761c253e8106cca0e1a28e275aaefbe7b93117b8cfd3ba2e0991822`) was rechecked. Its partial static member inventory does not prove insertion execution. Prior unreliable native reference captures are not repeated. No proprietary code or artwork is used.

## Acceptance before implementation

- Extend the existing Add to Timeline at Playhead context action to selected current bin entries, in visible bin order, once per entry. Insert adjacent clips on track 1 starting at the unchanged playhead. Still images use the existing five-second insertion duration; audio/video use source duration.
- Validate the complete batch before making a checkpoint: canonical current entries, existing source files, supported kinds/metadata, finite nonnegative start, valid unlocked track and total endpoint at most 24 hours. Reject invalid/stale/missing batches atomically. Empty selection and busy state are no-ops.
- Insert into Main Timeline in one checkpoint, select every new clip, preserve existing clips/groups/markers/track states and source bytes. Keep the independent editor's existing overlap policy: insertion does not ripple existing media. Configure a previously empty/unconfigured project from the first visual source using existing dimension limits.
- Undo removes the whole batch and restores initial dimensions; redo restores it exactly. Save/reopen, actual preview and decoded export agree for a two-source sequence. Single-item insertion remains supported, and sorted/filtered views determine visible ordering.
- Exercise actual context handlers and invalid/no-op states, run full gates, install, rerun against the exact installed executable, publish and verify the release identities.

## Independent choices / remaining gaps

This increment provides batch insertion through the existing context command. Visible bin order, first-visual configuration and overlap behavior are independent policies. Multi-item dragging, exact native ordering/collision behavior, canvas batch drops, configurable image duration and frame rounding remain open. Current single-item drag and single-item source preview retain their existing behavior; no unsupported batch drag control is presented.

## Focused validation

39 additional checks pass (MediaBinTest230). Actual WPF context insertion produces blue then red clips in visible ascending order and reverses source order with descending sorting. Preview pixels and decoded export agree. Validation covers stale/null members, missing later source, invalid metadata/time/track, lock and total endpoint overflow without mutation. Editor tests cover empty/busy/failed insertion without checkpoints, one-step undo/redo, exact save/reopen, initial canvas configuration and retained configured dimensions, existing clips/groups, Main Timeline selection, and source bytes. These tests exercise our source and are not current reference-runtime observations.
