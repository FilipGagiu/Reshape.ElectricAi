# Claude project instructions

> **The following phases are NON-NEGOTIABLE when starting a task that would result in code modification:**
>
> 1. **Invoke task-specific superpowers skill(s)** — match the task to a skill from §7. Fire BEFORE entering plan mode. Named mappings:
>    - New feature / behavior change → `superpowers:brainstorming`
>    - Bug, test failure, unexpected behavior, build failure → `superpowers:systematic-debugging`
>    - Implementation that admits unit tests → `superpowers:test-driven-development`
>    - About to claim "done" / "fixed" / "passing" → `superpowers:verification-before-completion`
>
>    If none of the named mappings fit, scan the full installed superpowers skill list for any skill that might help. If still nothing fits, proceed without one — that's an acceptable outcome, but document the full-list-scan result in the plan's Phase 1 application note. Silent skipping is not acceptable.
> 2. **Enter plan mode** (`EnterPlanMode`) — before ANY file edit. No exceptions for "small" or "trivial".
> 3. **Inventory / explore** — gather facts via Explore agents (parallel where useful) or direct reads. Do not guess.
> 4. **Design** — propose specific custom agents for review, exploration, or design feedback (NOT implementation — see §2). Review-agent dispatches MUST include "verify CODE.md compliance against the changed files" as an explicit directive. Recommend; do not decide unilaterally.
> 5. **Write the plan** to `.claude/plans/<slug>.md`. **Every plan MUST start by restating this phase list verbatim** so no phase is silently skipped.
> 6. **`ExitPlanMode`** — the single approval gate. Wait for explicit user approval.
> 7. **Execute** — YOU edit the files; only dispatch agents for review or parallel exploration. **Re-read [CODE.md](CODE.md) before each code edit** and verify the change honors every rule there. After approval.
> 8. **Verify** — build + tests + visible evidence. No "trust me" claims.
> 9. **Promote learnings to memory** — `/si:remember` for facts; direct-edit CODE.md (code rules), CLAUDE.md (workflow), or PROJECT.md (project context) for enforced rules. Penultimate step.
> 10. **Delete the plan file** — last step. Code + commit history is the source of truth after.
>
> If you catch yourself about to skip any phase, STOP. Re-read this list. The user has had to remind me of this repeatedly — that is the failure mode this section exists to prevent.

---

## Hook enforcement (do not duplicate in prose)

The following rules are now ALSO enforced mechanically by Claude Code hooks defined in [`.claude/settings.json`](.claude/settings.json) (scripts under [`.claude/hooks/`](.claude/hooks/)). The prose elsewhere in this file still documents intent; the hook is the authoritative enforcement point. If you change one of these rules, change the corresponding script — drift between prose and hook is a bug.

| Rule | Hook event | Script | Behavior |
|---|---|---|---|
| Session-start doc load (`CODE.md` / `PROJECT.md` / `README.md` / `.claude/docs/STATE.md`) | `SessionStart` (startup\|resume) | `load-project-docs.sh` | Injects file contents via `additionalContext`. Docs > 400 lines truncated with notice. |
| Bootstrap warn when `.claude/docs/` missing | `SessionStart` (startup\|resume) | `load-project-docs.sh` | Emits `systemMessage` telling Claude to offer scaffolding. |
| Caveman-mode confirmation on fresh session | `SessionStart` (startup) | `caveman-confirm.sh` | Injects instruction: ask user about caveman mode on first turn unless user already set it. |
| Date + git branch context | `UserPromptSubmit` | `inject-context.sh` | Adds `Date: YYYY-MM-DD\nBranch: <branch>` to every prompt. |
| Block `MEMORY.md` direct-edit (§3a) | `PreToolUse` (Edit\|Write) | `code-edit-guard.sh` | Exit 2 (case-insensitive); tells Claude to use `/si:remember` instead. **Exception:** the auto-memory index at `~/.claude/projects/<slug>/memory/MEMORY.md` is allowed (that IS what `/si:remember` writes to). |
| Re-read CODE.md before code edits (Phase 7) | `PreToolUse` (Edit\|Write) | `code-edit-guard.sh` | Injects reminder for `*.cs / .csproj / .sln / .ts / .tsx / .js / .jsx / .py / .go / .rs / .java / .kt`. Skipped for `.md` / config files. |
| Block package-install bash (§6a) | `PreToolUse` (Bash) | `bash-guard.sh` | Exit 2 on `dotnet add package`, `npm install <pkg>` / `npm i <pkg>`, `pip install`, `cargo add`, `yarn add`, `pnpm add`. |

**Cross-platform note:** scripts require `bash` (git-bash on Windows works) and `python` (3.x). No `jq` dependency.

**Composition:** the user-level caveman + superpowers SessionStart / UserPromptSubmit hooks still fire — Claude Code arrays them. Project hooks add to, not replace, user hooks.

---

This file is the standing instruction set: **how you work in any project that adopts it**. It is project-portable — the workflow rules, subagent strategy, PM handoff, and memory-promotion discipline below are reusable across projects.

**At session start, read all three:**

- **[`CODE.md`](CODE.md)** — the **code rulebook**: every rule that applies when you write or edit code. Re-read before any code change. Every review-agent dispatch MUST include a directive to verify CODE.md compliance.
- **[`PROJECT.md`](PROJECT.md)** — the **project context**: layout, build/test commands, data model split, expected warnings, plan/handoff files, navigation pointers. Read before answering questions about architecture, file placement, or commands.
- **[`README.md`](README.md)** — the **catalog**: system inventory (REST/gRPC/GraphQL/WebSocket surface, JSON schemas for canonical endpoints, architecture diagram, data sources, neighbor API dependencies). Read before answering "what exists?" or any integration/consumer question.

For per-task state, also read:

- **[`.claude/docs/STATE.md`](.claude/docs/STATE.md)** — at most ONE task, written by the `Senior Project Manager` subagent when the user requests an end-of-day handoff. Read at session start; if empty, no pre-queued work — wait for the user's first request.

## Bootstrap (fresh project / missing docs)

On session start, if `.claude/docs/` does not exist (or `STATE.md` / `todo.md` inside it is missing), **offer to scaffold the missing files with their canonical headers** and wait for explicit user confirmation before creating anything. Do not silently create files — some projects may not yet have adopted this workflow.

The canonical headers (used only when scaffolding):

- `STATE.md` — `# Next task\n\n> Written only by the Senior Project Manager subagent on user request. At most one task. Empty otherwise.\n\n_No task queued._\n`
- `todo.md` — `# Task Tracking\n\nGranular checklist for the **active** task.\n`

---

# Workflow Orchestration

## 1. Plan Mode is Mandatory

- **Before any file change, you MUST present a plan and wait for explicit user approval.** No exceptions for "small," "obvious," or "trivial" fixes — the user reviews everything before it lands.
- **Every plan file MUST open with the non-negotiable phase list from the top of this file, restated verbatim.** This is the self-check that prevents phase-skipping. A plan that does not restate the list is incomplete — fix at plan review.
- All plan-mode mechanics (entering, approval gate, memory-promotion timing, file deletion) are codified in the phase list at the top of this file. Bullets below add detail not in the phase list.
- Plans must be detailed enough to remove ambiguity: which files change, which custom agents will execute parts, which tests verify the outcome, what order the steps run in.
- Use plan mode for verification steps too, not just building (e.g. "run these tests, inspect this log, check this scene").
- If something goes sideways mid-execution, STOP and re-plan immediately — don't keep pushing through.
- **Plan files live at `.claude/plans/<slug>.md`** (project-local — visible to git, discoverable by future you / a reviewer). If the harness writes the initial plan to a global path, `Read` it after `ExitPlanMode` and `Write` it to `.claude/plans/` before starting implementation.

## 2. Subagent Strategy — ADVISORY ONLY

- **You write the code, not agents.** The main loop is the implementer. Agents are for parallel research, exploration, second opinions, and code review — never for primary implementation. If you're about to dispatch an agent with "implement X", "write the file Y", or "make this fix", STOP and write the code yourself.
- **Use agents for:**
  - Codebase exploration when scope is broad (`Explore` agent with parallel queries — keeps the main context clean)
  - Independent review of work you've already completed (`Code Reviewer`, `Security Engineer`) — **every review-agent dispatch MUST include "verify CODE.md compliance" as a directive**. A review pass that doesn't check CODE.md is incomplete.
  - Design feedback / second opinions during planning, then YOU implement
  - Parallel research that would otherwise bloat the main context window
- **Do NOT use agents for:**
  - Writing source / test / configuration files
  - Running the build or tests on your behalf (run them yourself, read the output, decide)
  - Editing files that the main loop should be editing directly
- Plans still PROPOSE agents — but for review, exploration, or design feedback, not implementation.
- Common picks: `Explore` (broad codebase search), `Plan` (design alternatives), `Code Reviewer` (post-work review), `Security Engineer` (security review), `Senior Project Manager` (STATE.md handoff). Project-specific picks in [PROJECT.md](PROJECT.md).

## 3. Self-Improvement Loop

- **Proactive capture (do this AS you work, not at the end).** The moment you discover something non-obvious — a codebase convention, a tool quirk, an architectural decision rationale, the canonical answer to "where does X live?" — capture it immediately via `/si:remember` (or direct edit of MEMORY.md). Examples: *"DbContext is `ListingDbContext` in Infrastructure, not Core"*, *"the canonical channel join key is `ChannelName` not `ChannelId`"*, *"the AutoMapper CVE warning is pre-existing — do not flag as new"*. **Bar for capture:** would the next session waste time rediscovering this? If yes, capture. Do NOT defer to end-of-task — by then you'll have forgotten half of it.
- **After ANY correction from the user:** capture the pattern in MEMORY.md immediately via `/si:remember` if it's a fact, or by editing the relevant memory file directly. The harness auto-loads MEMORY.md so the next session sees it.
- If the correction is an enforced rule (not a one-off observation), graduate it to `CLAUDE.md` (workflow), `CODE.md` (code rules), `PROJECT.md` (project context), or `.claude/rules/<topic>.md` (path-scoped) via `/si:promote` or direct edit (see §3a).
- Same gotcha in 2+ sessions → promotion candidate. Graduate it.
- The legacy `.claude/docs/lessons.md` is archived (`lessons-archive.md`) — do NOT add new entries there. See §3a below for the canonical knowledge sinks.

## 3a. Memory & Promotion (self-improving-agent skill)

The `self-improving-agent` plugin is installed. It adds slash commands for
curating Claude's auto-memory into durable project knowledge:

- `/si:status` — memory health dashboard (line counts, capacity, stale refs)
- `/si:review` — surface promotion candidates, stale entries, consolidation opportunities
- `/si:promote <pattern>` — graduate a learning from MEMORY.md → `CLAUDE.md` or `.claude/rules/<topic>.md`
- `/si:remember <fact>` — explicitly save important knowledge to auto-memory
- `/si:extract <pattern>` — turn a recurring pattern into a standalone reusable skill

### Knowledge sinks — pick the right home

| Sink | Owner | Scope | When to use |
|---|---|---|---|
| `MEMORY.md` (auto) | Claude (auto) | Project, NOT checked in | Background observations — auto-captured |
| `CLAUDE.md` | You + me (manual, or `/si:promote`) | Project, checked in | Enforced workflow rules every session loads |
| `CODE.md` | You + me (manual) | Project, checked in | Enforced code-writing rules — every code edit honors these; review agents verify compliance |
| `PROJECT.md` | You + me (manual) | Project, checked in | Project context — layout, commands, data shape, plan files. **Not code rules** (those live in CODE.md). |
| `.claude/rules/<topic>.md` | You + me (or `/si:promote --target rules/...`) | Path-scoped, checked in | Rules that only apply to specific files |

### Workflow integration

- **After the final step of any approved plan** (whether or not the work has been committed): run `/si:review` to surface anything auto-memory caught that's ripe for promotion. **"Shipped" is not the trigger — plan completion is.** This fires even when the user is reviewing the diff manually before commit, or has set a no-commit rule for the session.
- **Proactively during work** (per §3 first bullet): the moment you learn a non-obvious fact, invoke `/si:remember` via the Skill tool. Do not batch — capture inline.
- When you correct me on something I should never forget: capture via `/si:remember`. If it's an enforced rule, invoke `/si:promote` to graduate it to `CLAUDE.md` (workflow) or `.claude/rules/<topic>.md` (path-scoped). For code rules destined for `CODE.md` or project context destined for `PROJECT.md`, run `/si:remember` first (audit trail), then direct-edit the target file (the `/si:promote` skill currently targets only `CLAUDE.md` and `.claude/rules/`).
- Same gotcha in 2+ sessions → it's a promotion candidate. Graduate it.
- Path-scoped patterns (e.g., "all `EntityNode` subclasses must add behaviors before `base._Ready()`") that ONLY apply to certain file types belong in `.claude/rules/`, not the top-level docs, so they only load when relevant.

### Slash-command discipline (do NOT bypass)

The `/si:remember` and `/si:promote` slash commands exist for curation, deduplication, and audit trail. Per-file rules:

- **`MEMORY.md`** — NEVER direct-edit. Always `/si:remember <fact>`.
- **`CLAUDE.md`** — graduate via `/si:promote`. Direct-edit only when the user explicitly asks for a CLAUDE.md edit.
- **`.claude/rules/<topic>.md`** — graduate via `/si:promote --target rules/<topic>.md`. Same direct-edit exception as CLAUDE.md.
- **`CODE.md`** — `/si:promote` doesn't target it. Workflow: `/si:remember` first (audit trail), then direct-edit `CODE.md`.
- **`PROJECT.md`** — `/si:promote` doesn't target it. Workflow: `/si:remember` first (audit trail), then direct-edit `PROJECT.md`.

Slash commands run duplicate detection, capacity warnings, and rule-vs-observation hints that direct edits skip. Fall back to direct edit only if the slash command is broken — and surface the failure so it can be fixed.

## 4. Verification Before Done

- Never mark a task complete without proving it works.
- Run tests, check logs, demonstrate correctness.
- Diff behavior between main and your changes when relevant.
- Ask yourself: "Would a staff engineer approve this?"
- If something cannot be verified from CLI (e.g. game-engine scene visuals), say so explicitly rather than implying success.

## 5. Demand Elegance (Balanced)

- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: stop, use everything you know, implement the elegant solution.
- Skip this gate for simple, obvious fixes — don't over-engineer.
- Challenge your own work before presenting it.

## 6. Autonomous Bug Fixing

When given a bug report: fix it, don't ask for hand-holding. Point at logs / errors / failing tests, then resolve. Plan-mode rule still applies (present the fix as a plan first). Phase 1's `superpowers:systematic-debugging` governs HOW you investigate.

## 6a. Permission Boundaries — Ask, Don't Act

Some operations are outside your authority by default. When you need one, STOP and ask the user — do not work around the boundary or attempt to derive the answer yourself.

- **Package installs.** You may NOT add NuGet, npm, pip, cargo, or any other package-manager dependency. If the work requires a new package, surface what you need (name, version, target project, why) and wait for the user to install it. After they confirm, continue. This applies even to "obviously safe" or "trivial" packages — no exceptions.
- **Namespace / `using` lookup.** When a type is unresolved and `Grep` across the project's source files returns zero hits, the type lives in an external assembly. Do NOT spelunk `~/.nuget/packages/`, `node_modules/`, vendor directories, or compiled DLLs to find the namespace. Ask the user — they know the answer faster than you can derive it. Filesystem spelunking for namespaces is slow, error-prone, and not your job.
- **Pattern when stuck:** state precisely what's missing and ask. E.g. *"I need package `HtmlSanitizer 8.0.871` in the Presentation csproj — can you install it?"* or *"I can't find a `using` for `IFooService` in the project source — what's the namespace?"* Don't apologize, don't workaround, don't guess. Just ask.

## 7. Superpowers Plugin — Integration Rules

The `superpowers@claude-plugins-official` plugin (v5.1.0, community, Jesse Vincent) is installed. Its `SessionStart` hook auto-loads the `using-superpowers` bootstrap, which auto-triggers other skills. To prevent conflicting rituals with the existing workflow, use it **selectively**:

### Use Superpowers skills for:
See Phase 1 of the top-of-file phase list for the canonical mapping. Notes that don't fit there: `systematic-debugging` supersedes §6 in HOW you investigate (§6 still governs "no hand-holding"); `test-driven-development` is exempt for game-engine integration layers where unit tests don't run in isolation (see [PROJECT.md](PROJECT.md)).

### Defer to existing equivalents (do NOT use Superpowers' version):
| Superpowers skill | Use this instead |
|---|---|
| `executing-plans`, `writing-plans` | Mandatory plan-mode workflow + `.claude/docs/todo.md` + `Plan` subagent |
| `requesting-code-review`, `receiving-code-review`, `subagent-driven-development` | `Code Reviewer` custom subagent + `/review` + `/ultrareview` |
| `writing-skills` | `anthropic-skills:skill-creator` and `/si:extract` |
| `dispatching-parallel-agents` | Already mandated by §2 — redundant framing |
| `finishing-a-development-branch` | Plan-mode cleanup (§1 plan-file deletion) + optionally invoking the PM agent to queue the next task |
| `using-git-worktrees` | `Plan` / agent `isolation: "worktree"` parameter when wanted |

### Precedence when rituals conflict:
Plan mode (§1) is final — brainstorming feeds in, never around. Custom subagents (§2) win over Superpowers' subagent skills. PM handoff stays opt-in. `/si:*` and Superpowers operate independently.

---

# PM handoff (STATE.md)

[`.claude/docs/STATE.md`](.claude/docs/STATE.md) holds **at most one task** — the next thing the next Claude Code session should pick up. It is written exclusively by the `Senior Project Manager` subagent, exclusively when the user asks for it.

## When to invoke the Senior Project Manager subagent

**Only when the user explicitly asks for it.** Examples of explicit asks:

- "Prep a task for tomorrow's session."
- "Write up a handoff spec I can come back to."
- "Queue this as the next task."
- "Plan tomorrow's work" (the words "plan" + "tomorrow" / "next session" combined).

**Never auto-invoke**, even when starting a substantial feature. The user runs the iterative loop directly through plan-mode (`EnterPlanMode` / `ExitPlanMode`) and the `Plan` subagent — the PM-agent handoff is reserved for end-of-day queueing.

When invoked, the PM agent **overwrites** STATE.md with a single task block in the shape below. There is no append, no history, no list — at most ONE task at a time.

## Required `STATE.md` task shape

Defined in the `Senior Project Manager` agent itself (title / summary / background / acceptance criteria / out of scope / references). The block is overwritten on each PM invocation. When the next session ships the task, STATE.md is reset to `_No task queued._`.

## Anti-bloat

- STATE.md holds AT MOST one task. Never a list, never a history.
- No COMPLETED.md, no shipped-work log — `git log` is authoritative.
- Plan files live at `.claude/plans/<slug>.md` during the work and are deleted on completion (see §1).

---

# Core Principles

- **Simplicity First** — make every change as simple as possible. Impact minimal code.
- **No Laziness** — find root causes. No temporary fixes. Senior-developer standards.
- **Minimal Impact** — changes only touch what's necessary. Avoid introducing bugs.
- **Custom Agents — advisory only** — agents are for exploration, review, and design feedback. YOU write the code. See §2.
