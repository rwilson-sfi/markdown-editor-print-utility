# Project Status

- Status: Active
- Updated: 2026-09-02 21:24 ET
- Current service: Claude Code / Claude Fable 5
- Current host: SFI-P14-2255
- Repository boundary: Independent local repository; active development worktree at
  `C:\SFM\Dev\MarkdownEditor`
- Branch: `host/sfi-p14-2255`
- Last runtime-verified implementation commit: `af6ccba0d39c7d6b0721b1b8e99f03a0cf227992`
- Latest build-verified implementation commit: `28df687` (single-instance hand-off fix)
- Working-tree state: Clean at closeout

## Current state

The source now includes, beyond the runtime-accepted Explorer/tabs/single-instance milestone and the
eleven-palette Hermes gallery: compact editor typography and softened surfaces (`12a1c5f`, `898ca15`),
print typography and margin fixes (`8409fe8`, `f1e593e`), external-change detection, recovery drafts,
and recent files (`7c2e652`), and a hardened single-instance hand-off (`28df687`). Those commits from
2026-08-29 through 2026-08-31 were made without worklog, TIMELOG, or STATUS updates; this closeout
reconciles STATUS to the verified repository state but does not reconstruct their session records.

2026-09-02 investigation of the user-reported print failure found no defects in the print path.
The failures were (1) secondary launches crashing unhandled when the running primary's pipe was owned
by a different security context (fixed in `28df687`: every hand-off failure now shows an actionable
dialog, and the primary listener survives a foreign-owned pipe name), and (2) host application
control denying first execution of freshly built unsigned assemblies in both `C:\AI-Workspace` and
`C:\SFM\Dev`. `28df687` builds with zero warnings and passes formatting verification; test execution
and runtime acceptance remain blocked by application control. A healthy-path hand-off regression with
the allowlisted pre-fix binaries passed (secondary exit 0, one process, file forwarded). No remote is
configured.

## Next action

When the host admits fresh assemblies: run the automated suite, runtime-accept `28df687` (including a
two-process smoke against a foreign-owned pipe) and the pending gallery/typography/recovery commits,
then continue the implementation plan.

## Blockers

- Host application control denies first execution of freshly built unsigned assemblies in
  `C:\AI-Workspace`, temp locations, and the approved `C:\SFM\Dev\MarkdownEditor` worktree
  (recorded 2026-08-24 through 2026-09-02). Compilation succeeds; runtime acceptance requires policy
  admission or a later signed build. Do not weaken or bypass endpoint-security controls.
- Do not run the editor elevated: an elevated primary owns the single-instance pipe and normal
  launches cannot connect to it (now reported by dialog instead of crashing).
- Remote creation, publication, and cross-host workspace registration remain deferred.
- Registering the project in the current `WORKSPACE.md` dashboard before its GitLab repository exists
  would make `scripts/clone-rw-ws-01.sh` attempt a nonexistent remote and fail.
- Signed packaging requires a signing identity/certificate in a later release gate.
- Live Explorer-verb or sign-in startup registration remains separately approval-gated; no registry
  or startup state has been changed.

## Handoff notes

The product behavior and technical architecture are approved and recorded. The current code is a
development milestone, not a release. Folder navigation, tabs, single-instance transport, and the
original five-palette interface are runtime-accepted; the eleven-palette gallery, typography/print
styling, external-change/recovery/recent-files slice, and the hand-off fix are build-verified and
pending host-policy runtime acceptance. It does not yet install Explorer verbs, startup behavior, or
a signed package. Continue from [docs/IMPLEMENTATION-PLAN.md](docs/IMPLEMENTATION-PLAN.md). The local
repository has no remote; do not add one or publish without explicit destination approval.
