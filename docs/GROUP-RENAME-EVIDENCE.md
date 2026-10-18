# Group naming: revision 0.0.10

## Evidence and scope

The controlled Camtasia group context menu observation retained in GROUPING-EVIDENCE.md exposes Rename Group alongside Open group and Ungroup. This increment completes that missing entry point in TatsiaCraft. Exact reference rename-dialog behavior, limits and keyboard shortcuts have not been measured; the dialog and validation below are independent design choices.

REA retained WPFSharedFiles.dll member evidence ev_214d7b88642e64fa88f80eeabf5d370074743ea71b9c2287711a51c033b74b2c (SHA256 46b37ce47761c253e8106cca0e1a28e275aaefbe7b93117b8cfd3ba2e0991822) was reused through its evidence-bundle summary. Partial managed member coverage does not locate timeline group naming. No repeat failed analysis, proprietary source or assets are needed.

## Acceptance before edits

Offer Rename group from the selected complete group's properties, Edit menu and timeline context menu. Show the existing name in an owned modal with Rename and Cancel. Accept nonblank Unicode names up to 200 characters after trimming surrounding whitespace. Invalid input remains in the dialog with an explanatory error; Cancel and unchanged names leave project, undo stack and dirty state untouched. Reject ambiguous mixed selections, missing groups, busy editor and locked member tracks before any edit. A successful rename changes only the target group's name, creates exactly one undo checkpoint, updates the collapsed block/member-context label and survives save/reopen. Preview, decoded export, source media, member identities/timing and geometry must remain unchanged. Existing flat-group limitations remain open. Verify the installed workflow before publishing the exact executable.

## Focused verification

The grouping suite passes 223 checks, adding 37 naming checks: Unicode/normalization/limits, no-op and invalid atomicity, locked siblings, unchanged preview/decoded export, exact save/reopen, editor selection guards, dirty/undo protection, one undo/redo and retained member context. Dialog Cancel, invalid-input feedback and installed menu/property entry points require the release's installed runtime smoke check. Group names are clipped within their timeline block; the tooltip retains the full name. Full reference dialog parity remains unverified.
