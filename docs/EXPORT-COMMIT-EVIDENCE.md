# Export commit recovery: revision 0.1.3

## Observed defect and evidence boundary

During the 0.0.10 release preparation, the existing resize regression failed in Engine.Render at File.Replace with IOException: "Unable to remove the file to be replaced." The retained log is artifacts/release-0.0.10-prepare.log. Its focused rerun passed480 checks, and a full rerun passed; that recovery does not identify the lock holder. Inspection at0.1.2 confirms export still commits with an immediate File.Replace/Move, while project Save already retries only HRESULTs32,33and1175. This is an independently reproduced robustness repair, not a claim about Camtasia internals. Existing REA group metadata cannot establish reference export commit behavior; no identical failed binary analysis is repeated.

## Acceptance before edits

Keep rendering to a unique temporary MP4 in the destination directory. Commit it atomically using the existing bounded retry policy (five attempts, delays100/200/400/800ms, only HRESULT32,33,1175). Reuse the commit helper for Save without changing its behavior. Report Finalizing export before commit, and Export complete only after successful publication. A short-lived real Windows no-delete destination lock must recover; a permanent lock must fail within the bounded commit window while retaining original destination bytes and cleaning temporary files. Missing/read-only/invalid destinations must fail honestly. Preserve source media, project state and decoded output/duration/audio. Test new and replacement exports and the installed workflow before publishing.

## Focused results

24 checks passed using real Windows destination handles: temporary lock recovery, permanent-lock bounded failure, previous bytes retained, no false completion, render-temp cleanup, read-only/missing destinations, Unicode new/replacement paths, source/model preservation and decoded video pixels/audio/duration. The historical 1175 failure's process owner is unknown; the deterministic lock scenario produces a sharing violation. No runtime-equivalence claim is inferred from either metadata or matching output alone.

## Release source identity

During verification an unrelated, untracked src/Behaviors.cs appeared and the wildcard compiler input picked it up, causing missing-member compiler errors. It is preserved. Release builds now enumerate C# sources from the recorded staged tree and reject index or tracked-working-file drift at each build. Ordinary development builds continue to include new files. This prevents publishing source that is absent from the staged Git archive; it does not treat the unfinished behavior implementation as tested or complete.
