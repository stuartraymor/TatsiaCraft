# Media Bin / timeline navigation

## Evidence and limits

TechSmith's [Windows 2020 version history](https://support.techsmith.com/hc/en-us/articles/35532296446733-Camtasia-Windows-2020-Version-History) documents Select on Timeline from the Media Bin, finding/selecting a timeline source in the bin, and a subsequent fix to Select in Media Bin scrolling. This establishes the command workflow historically, not the exact current 26.2.2 selection/scroll policy.

REA retained WPFSharedFiles.dll member evidence `ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c` was rechecked (SHA-256 `46b37ce47761c253e8106cca0e1a28e275aaefbe7b93117b8cfd3ba2e0991822`). Its partial static coverage does not prove timeline/bin execution or group behavior. Native reference capture remains unreliable; identical failed probes are not repeated. No reference assets or implementation are copied.

## Acceptance before implementation

- Media Bin context Select on Timeline locates all references to selected current media IDs, including split/separated streams, hidden/locked tracks and missing source files. Alias paths with different IDs remain distinct. Multiple bin entries form a union; empty/stale/unused-only selections and busy operations preserve the prior timeline selection.
- Return to Main Timeline when a match is found; expand matching members to complete root group units, retaining current flat-group editing invariants. This is an independent policy. Do not change head, playback, project data, dirty/undo/redo, track states or source files. Reveal the first resulting timeline unit without seeking.
- Timeline context Select in Media Bin reveals the Media tool, resets its type filter to All media, selects unique source references of the current timeline selection and scrolls to the first. Preserve Name/Type direction/layout. Ignore text-only clips and unknown references. No matches or busy state preserve the current bin view and selection.
- Expose real enabled/disabled commands; verify actual context handlers, union/group/stream/alias behavior, cross-project stale references, large timelines and filtered bins, no-op states, save/reopen and main preview/export preservation.
- Run complete regression gates, install, rerun against the exact installed assembly, then publish and verify source-tree/commit/tag/asset identity.

## Open parity checks

Exact current native menu position, multi-selection policy, group navigation, hidden/locked selection rules, focus transfer and scrolling offsets remain unmeasured. This increment reproduces a documented workflow with explicit independent selection policies; it does not establish complete reference parity.

## Focused validation

38 added checks pass (MediaBinTest191): pure source/reference identity and stale guards; actual context commands; group-unit union selection including hidden/locked tracks; separated streams; returning to Main Timeline; scrolling to a distant hidden clip; reverse navigation through filters with sort/layout retained; unused/empty/text/busy no-ops; missing-file lookup; playback/head and undo/redo preservation; identical project save/reopen and main preview/decoded export. Existing source-byte checks run after the new workflow. Source-owned WPF checks are not observations of the reference runtime.
