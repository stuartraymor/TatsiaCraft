# Media-bin sorting and selection: revision 0.1.6

## Evidence boundary

TechSmith's Camtasia Help2022, Media Bin Tasks (printed page36), documents a Sort by dropdown and an Ascending/Descending button: https://www.techsmith.com/blog/wp-content/uploads/2023/12/Camtasia-Help-2022-EN.pdf . This supports the direction control workflow. Exact26.2.2 sorting collation, option catalog, default direction and selected-entry behavior have not been measured. Current native capture is not trusted after previously returning another foreground application, so no new native screenshot is claimed.

REA retained WPFSharedFiles.dll member evidence ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c (SHA25646b37ce47761c253e8106cca0e1a28e275aaefbe7b93117b8cfd3ba2e0991822) is reused through its summary. Partial selection/file-picker inventory does not establish media-bin sort semantics. No proprietary assets/code or repeated failed analysis are used.

## Acceptance before implementation

Extend the existing Name/Type sort dropdown with a functional adjacent Ascending/Descending toggle, descriptive tooltip and automation ID. Keep ordering stable, deterministic and case-insensitive (independent ordinal collation); Type uses name as its secondary key, with both keys reversed in descending mode. Combine filtering and sorting without mutating project.Media, source metadata, timeline, undo/dirty state or export. Preserve selected visible media IDs through sorting, thumbnail/details layout changes, refreshes and tool switches on the same project. Filtering out an entry clears that hidden selection; opening a different Project instance clears all bin selection even if IDs match, avoiding stale Relink/Details targets. Retained single selection must keep Relink eligible; empty/multiple selection must disable it. Reject invalid view arguments and handle empty/Unicode/tied names. Do not add unsupported sort fields or claim exact reference collation/layout. Verify actual direction/filter/view buttons and installed selection workflow, source bytes, save/reopen and unchanged preview/export.

## Focused results

29 additional checks pass (media-bin suite75): stable ascending/descending Name/Type ordering, Unicode and null/tied names, filtered/empty/invalid views; actual direction/dropdown/layout controls; selection retention through tool switches and refresh; filtered-out and cross-project selection clearing; correct Relink eligibility; unchanged project/undo/dirty state, saved data, actual preview and decoded export pixels. Source-file preservation and existing cleanup/Details/keyboard guards remain covered by the original46 checks. Exact physical gestures, native sort collation and all keyboard-focus combinations remain unmeasured.
