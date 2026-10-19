# Leading and trailing timeline move snapping

## Evidence boundary

TechSmith's [Studio 8 Help](https://assets.techsmith.com/docs/pdf-camtasiastudio/camtasia_studio_8_help_file.pdf), page 93, documents snapping when the beginning or end of moving media aligns with another item. This supports both endpoints; it does not establish current 26.2.2 group bounds, priority, thresholds, modifier or guide styling. Existing settings evidence is in TIMELINE-SNAPPING-EVIDENCE.md.

Retained REA partial member evidence `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c` was rechecked. Static metadata cannot prove endpoint execution. Native runtime capture remains unreliable; identical failed probes and proprietary source/assets are not used.

## Acceptance before edits

- During active clip/group timeline movement, evaluate the leading and trailing bounds against enabled targets. Choose the smallest correction inside the strict eight-display-pixel threshold; ties prefer the leading edge and stable target order. Preserve offsets and gaps within selections.
- Exclude every moving clip from target generation; respect Media Edges/Playhead/Markers settings, source/target per-track flags, existing Shift bypass and operation lock/time/track clamps. Keep the previous start-only helper for callers that explicitly need a single position.
- Validate finite start/span/delta/zoom/targets. Bypass produces the original movement. A no-match result preserves the requested movement; an already aligned edge may have zero correction. Do not change trim behavior, project serialization or selection policy.
- Test both endpoints in both directions, nearest/tie/no-match/threshold/zoom behavior, wide/gapped/multiple/group bounds and disabled targets. Apply snapped movement through the actual editor move operation and verify one-step undo/redo, locks, save/reopen, source preservation and preview/export agreement.
- Run complete release gates, install and test the exact installed executable, then publish and verify commit/tag/source/asset identities. Physical pointer gestures and exact current reference endpoint/guide behavior remain pending.

## Focused validation

39 added checks pass (MediaBinTest378). Pure checks cover leading/trailing bounds in both directions, zero boundary, long spans, zero correction, nearest choice across endpoints, stable ties, display zoom threshold, bypass/no targets and invalid inputs. Editor checks use the same helper wired to timeline dragging and apply its delta through the actual movement operation; they cover external/group/marker/disabled targets, read-only planning, preserved gaps/source IDs/group membership/head, one checkpoint, locked guards, exact undo/redo/save and actual preview/decoded export at the new position. These source-owned checks do not measure a physical drag or current Camtasia runtime behavior.
