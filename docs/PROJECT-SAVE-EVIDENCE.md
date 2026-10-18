# Project-save robustness: revision 0.0.8

## Observed defect and bounded hypothesis

During the 0.0.7 generated resize UI test, Ctrl+S once reported 'Unable to remove the file to be replaced'. A manual retry succeeded; saved geometry was intact. The process that held the file was not identified. This does not establish Camtasia's internal behavior or the exact source of the original lock.

Before editing, a standalone controlled scenario against the installed 0.0.7 executable saved a generated text project, held its destination open with FileShare.ReadWrite (without delete sharing), and scheduled release after 300 ms. Engine.Save failed immediately, in 1 ms, with IOException HRESULT 0x80070020. Original bytes were unchanged and no temporary files remained. This reproduces a sharing violation, not the exact original UI error 0x80070497. Baseline inputs/code/log are retained under work/save-lock-repro outside the public source tree.

Hypothesis: short-lived Windows access conflicts can be recovered by retrying the atomic commit while retaining the same complete temporary file. A failed Save As must not change the active project path or clear the dirty marker.

## Acceptance before implementation

- Preserve the existing validated JSON format, same-directory temporary write and atomic File.Replace/File.Move. Never delete or directly truncate the existing project to work around a failed commit.
- Retry only known Windows IOException HRESULTs for sharing violation (0x80070020), lock violation (0x80070021) and unable-to-remove-replaced (0x80070497). Limit to five attempts with 100/200/400/800 ms waits. Other errors propagate, including permissions, read-only paths, disk/missing-directory failures and invalid projects.
- A real temporary lock released after 300 ms must let one save finish successfully, with valid new project data. A persistent lock must fail within a bounded interval and preserve the original bytes. Clean up only the uniquely named temporary file owned by that save.
- Save As uses a local candidate destination; active project path, dirty state and versioned title update only after Engine.Save succeeds. Cancel/failure retains the previous project and unsaved edits.
- Test ordinary new/repeated/Unicode saves, source overwrite protection, validation errors, permanent/transient locks, temporary-file cleanup, editor Save As state and save/reopen/export. Test the installed UI with generated projects before publishing the exact verified executable.

## Sources and evidence limits

Microsoft's [file-write guidance](https://learn.microsoft.com/en-us/windows/apps/develop/files/best-practices-writing-files) lists sharing violations and unable-to-remove-replaced failures as candidates for retry and synchronized access. Its examples concern WinRT transactional writes; the guidance motivates a bounded retry, not a claim of identical implementation. [ReplaceFileW documentation](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew) distinguishes replacement error states. Errors 1176/1177 are not treated as safe generic retry conditions.

REA is not run against our ordinary source-code failure path. Reuse the retained authorized reference identity and explicit Windows native-UI observer limitation in PARITY-PLAN.md. No metadata result establishes the reference's file-lock behavior; no new native reference equivalence is claimed. Crash/power-loss recovery, network/distributed filesystems, file identity aliases and multi-process conflict resolution remain unverified.

## Verified checks

60 independent checks passed. A real 300 ms no-delete-sharing lock recovered without a manual retry; a persistent lock failed after the bounded retry window. The tests verify original bytes on failure, unchanged in-memory state, uniquely owned temporary-file cleanup, Unicode new/repeated/reopened projects, invalid inputs, source protection, immediate nontransient failures, successful decoded export after recovery, and isolated Editor Save As path/dirty/title/status/recent-history behavior. The 1175 classification is tested with a synthetic exception; only error 32 was induced through a real Windows handle. The earlier live UI message provides separate evidence for 1175.

In a separate 0.0.8 editor session, moving generated media to X80/Scale0.5 created an unsaved edit. A helper held only that generated project open without delete sharing, observed the new save temporary file, waited 300 ms and released its handle. One Ctrl+S completed with 'Project saved', no error dialog or dirty marker, saved X80/Scale0.5 and no leftover save temporary file. The helper log records LOCKED, SAVE_STARTED, RELEASING_AFTER_300MS and RELEASED. No existing user projects or editor sessions were changed.
