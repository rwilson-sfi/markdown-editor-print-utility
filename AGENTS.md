# Project Working Agreement

Before any work, read `../AGENTS.md`; it is the canonical workspace agreement. This file contains only project-specific additions or overrides.

## Startup

- Read `README.md`, `STATUS.md`, `MEMORY.md`, the latest worklog, and relevant portions of `RUNBOOK.md` before substantive work.
- Read each workspace policy relevant to the requested action.
- For nontrivial work, provide the context receipt from `../policies/HANDOFF.md` before editing.

## Project-specific constraints

- This is a Shared, Normal-sensitivity utility. Keep it free of Security First-specific data and
  portable across employers and Windows hosts.
- Preserve user-authored Markdown. Switching between rich and source editing must not silently drop
  supported syntax, and printing must never modify the source file.
- Windows shell integration must be reversible and scoped to Markdown files.
- Do not add telemetry, cloud synchronization, accounts, or network dependencies without an explicit
  design decision.

## Verification

- Verify source/rich-editor round trips against representative Markdown fixtures.
- Verify formatted output through print preview and a PDF printer before testing a physical printer.
- Verify tray startup/exit behavior and reversible registration of any Windows Explorer context-menu
  entries on a supported Windows host.

## Approval gates

- Obtain user approval before making persistent system-wide registry changes or enabling startup at
  sign-in on a live host.
- Obtain user approval before adding telemetry, network services, automatic updates, or file-format
  associations beyond the requested Markdown context-menu commands.
- Apply all approval gates in `../policies/SOURCE-CONTROL.md` and `../policies/SECURITY.md`.

## Closeout

- Follow `../policies/HANDOFF.md`, update shared project state, and provide a handoff receipt.
