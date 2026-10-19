# Media-bin organization: revision 0.1.5

## Evidence boundary

TechSmith's Camtasia Help2022, Media Bin Tasks (printed pages36–37), documents right-click Details, Select Unused Media and Delete Unused Media. Source: https://www.techsmith.com/blog/wp-content/uploads/2023/12/Camtasia-Help-2022-EN.pdf . This establishes the documented workflow; exact26.2.2 dialog fields, ordering and selection gestures have not been re-measured. Native capture previously returned the foreground application instead of the selected editor, so no current native screenshot is treated as reference proof.

Retained REA WPFSharedFiles.dll member evidence ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c, SHA25646b37ce47761c253e8106cca0e1a28e275aaefbe7b93117b8cfd3ba2e0991822, is reused through its summary. Partial file-picker/selection members do not establish media-bin deletion or Details semantics. No proprietary code/assets or repeated failed analysis are used.

## Acceptance before implementation

Add functional bin context commands Details, Select Unused Media, Delete Unused Media. Determine usage from every project clip's media ID, including hidden/locked tracks, grouped members and separated stream halves; current filters must never determine whether media is unused. Selecting unused entries is a view operation: reveal all media, select all unused entries, preserve source/project/undo/dirty state. Delete Unused Media removes only project references to unused entries, never files, in one undo step, with redo/save/reopen and unchanged rendered composition. Empty/no-op/busy operations must not create checkpoints. Preserve still-used entries even when another unused entry points to the same file. Show honest empty-filter text. Details is read-only and shows stored name/type/path/dimensions/duration/audio, file availability and clip-reference count; these fields are independent choices. Relink requires exactly one entry; multi-item dragging/insertion remains explicitly unsupported rather than acting on an arbitrary member. Validate source-byte preservation, preview/decoded export, legacy projects, metadata and installed command behavior. Full native-dialog and media-bin layout parity remain open.

## Focused verification

The new suite contains46 checks: media-ID usage across grouped, hidden, locked and split-stream clips; same-path aliases; empty/all-unused bins and missing unused sources; actual preview and decoded export preservation; source bytes; whole-bin selection across filters without dirty/undo effects; documented context labels; single-entry Relink eligibility; read-only Details dialog; one-step cleanup undo/redo and exact save/reopen; honest filtered empty state; no-op/busy guards. Generated fixtures and isolated WPF windows are used; no user media or running editor is modified.
