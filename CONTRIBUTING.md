# Revisions and GitHub releases

Every completed revision must be committed, pushed to GitHub and published as a release. This requirement applies to human developers and coding agents. Use the release script rather than manually changing assembly version attributes.

1. Make the intended changes and stage the specific files with Git.
2. Run `powershell -ExecutionPolicy Bypass -File scripts/release.ps1 -Message "Describe the revision"` on an interactive Windows desktop with authenticated GitHub access.
3. Verify the printed GitHub release URL. A failed upload is unfinished work; resolve it before reporting completion.

The script starts at 0.0.1. Each position below major runs from 0 to 12: 0.0.12 is followed by 0.1.0, and 0.12.12 is followed by 1.0.0. Generated assembly/file versions add a fourth zero for Windows metadata; the visible product version remains three numbers.

GitHub authentication uses `GH_TOKEN` / `GITHUB_TOKEN` if supplied, otherwise the configured Git credential helper. Credentials are never written into the repository. Your account needs repository contents and release write access. Release assets contain the editor executable and source; FFmpeg dependencies remain a separate runtime requirement.

If a release fails after its Git tag was pushed, rerun with `-Resume -Message "Original revision description"` to finish that tagged revision instead of consuming a new version. Resume requires a clean checkout whose HEAD and VERSION match the tag. Never overwrite an already published release or reuse a version for different code.
