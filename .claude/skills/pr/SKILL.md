---
name: pr
description: Verify branch, run tests, self-review, push, create PR, monitor CI and review feedback, then merge when approved.
---

# /pr — Pull Request Skill

Automates the full PR lifecycle: branch verification, **pre-flight tests and self-review first**, then push and PR creation, then mandatory CI/review monitoring, and merge.

**Workflow order:** Step 1 → Step 2 (tests) → Step 3 (self-review) → Step 4 (push + create/update PR) → Step 5 (monitor). Do not open the PR until Steps 2–3 pass; the last substantive action before reporting to the user should be Step 5 (or its pending/failed outcome).

> **Note:** This skill is scoped to the Mental Metal repository (.NET backend, Angular frontend when present). Frontend steps are conditional — they are skipped automatically when the Angular project does not yet exist.

## Definition of done (non-negotiable)

The `/pr` workflow is **not complete** until **Step 5** has run **at least once** for the PR you opened or updated. Do **not** treat “PR created” or “tests passed locally” as the end state.

**Before you stop or tell the user the task is finished, you MUST:**

1. Capture `PR_NUMBER` (from `gh pr create` output, `gh pr view`, or `gh pr list --head <branch>`).
2. Run **`gh pr checks $PR_NUMBER`** (or `gh pr view $PR_NUMBER --json statusCheckRollup`) and report the result to the user.
3. Fetch review and comment signals **once** (inline review comments, PR reviews, issue comments) using the commands in Step 5b — or state that there are none yet.
4. If any check is **failed**: follow Step 5a (logs, fix, re-test, push) — do not stop green.
5. If any check is **pending**: say so explicitly, give the PR link, and say that CI/bots may still be running; offer to poll again or tell the user what to watch. **Do not imply everything is green** when checks are pending.

**Exception:** The user explicitly asks only to open the PR or “create the PR without waiting for CI” — then skip Step 5 and say that monitoring was skipped by request.

---

## Step 1: Verify Branch and Uncommitted Changes

1. Run `git branch --show-current` to check the current branch.
2. If on `main`, **stop and ask the user** for a branch name. Create a feature branch with one of these prefixes: `feat/`, `fix/`, `chore/`, `docs/`.
3. Check for uncommitted changes with `git status`. If any exist, stage and commit them with a descriptive message before proceeding. Stage specific files — never use `git add .` or `git add -A`.

---

## Step 2: Pre-Flight — Run Tests

Run available test suites **before** pushing or opening a PR. **Abort if any fail:**

1. **Backend tests** (always run):

   ```bash
   dotnet test src/MentalMetal.slnx
   ```

2. **Frontend tests** (only if `src/MentalMetal.Web/ClientApp/angular.json` exists):

   ```bash
   (cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)
   ```

   If the ClientApp directory does not exist, skip this step.

**STOP if any test suite fails.** Diagnose and fix the failures, then re-run this step. Do not proceed past this step with failing tests.

After fixing, **commit** locally. **Push** only in Step 4 (after Step 3).

---

## Step 3: Self Code Review

Review the full diff against main before pushing or opening the PR:

```bash
git diff main...HEAD
```

Check for:

- **Code duplication** — extract shared logic if the same pattern appears 3+ times
- **Missing error handling** — at system boundaries (user input, external APIs)
- **Security issues** — unsanitised input, exposed secrets, SQL injection, XSS
- **Codebase convention violations** — if `CLAUDE.md` exists, check it for banned patterns and required conventions; otherwise follow the repository conventions documented in this checklist
- **EF Core pitfalls** — `HashSet.Contains()` and `.ToLowerInvariant()` are not SQL-translatable; use `List<T>.Contains()` and `.ToLower()`
- **Angular pitfalls** — race conditions in async calls, timezone-safe date handling, plain properties instead of signals
- **Banned Angular patterns** — `*ngIf`, `*ngFor`, `*ngSwitch`, `[ngClass]` (must use `@if`, `@for`, `@switch`, `[class.x]="expr"`)
- **Banned colour patterns** — hardcoded Tailwind colours (`bg-gray-100`), custom `--color-*` CSS variables, `dark:` prefix
- **Boy Scout Rule** — fix banned patterns in lines you're already changing, but do NOT expand scope to unrelated code

Apply all fixes immediately. **Commit** before proceeding. **Push** in Step 4.

---

## Step 4: Push and Create or Update the PR

Run this **only after** Steps 2–3 pass. Opening the PR is what starts remote CI and bots; local quality gates should already be green.

1. Push all commits to the remote branch:
   ```bash
   git push -u origin <branch-name>
   ```
2. Check if a PR already exists for this branch and capture the PR number:
   ```bash
   PR_NUMBER="$(gh pr list --head <branch-name> --json number -q '.[0].number')"
   ```
3. If a PR already exists (`PR_NUMBER` is set), update it instead of creating a new one:
   ```bash
   gh pr edit "$PR_NUMBER" --body "$(cat <<'EOF'
   ...updated body...
   EOF
   )"
   ```
   If no PR exists yet, create it in substep (5) below, then capture the number:

   ```bash
   PR_NUMBER="$(gh pr view --json number -q '.number')"
   ```
4. If `openspec/specs/` exists, look for a spec related to this branch. If the directory does not exist, skip this step.
5. **Create** the PR (only if none exists) using this template:

   ```bash
   gh pr create --base main --title "<short title under 70 chars>" --body "$(cat <<'EOF'
## Summary

<1-3 sentences describing what this PR does and why>

**Spec**: [<spec-name>](openspec/specs/<spec-name>/spec.md)

## Changes

- <bullet list of key changes>

## Test Plan

- [ ] `dotnet test src/MentalMetal.slnx` passes
- [ ] `ng test --watch=false` passes (if frontend exists)
- [ ] <feature-specific verification steps>

---
🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
   ```

   - If no OpenSpec spec is found (or `openspec/specs/` does not exist), omit the **Spec** line entirely.
   - Review `git log main..HEAD` and `git diff main...HEAD` to write an accurate summary and change list.

6. **Set `PR_NUMBER`** for the rest of the workflow (`gh pr view --json number -q '.number'` or parse from `gh pr create` output). You need it for **Step 5** — do not end the session without running Step 5 (see Definition of done).

---

## Step 5: Monitor CI and Review Comments (Review Loop) — **MANDATORY**

Do this **in the same session** as Step 4 unless the user opted out (see Definition of done). Skipping this step is a failure to follow the skill.

After the PR exists (Step 4), actively monitor and address feedback.

### 5a. Wait for CI

Poll CI status until all checks complete **or** until it is clear some checks are still pending after a reasonable wait (see below):

```bash
gh pr checks $PR_NUMBER
```

**Polling:** If checks are pending, wait **60–120 seconds** and re-run `gh pr checks` up to **5 times** (~10 minutes). If still pending, report status and stop — do not claim all checks passed.

If CI fails:
1. Identify the failed check(s) and extract the run ID from the link field:
   ```bash
   gh pr checks $PR_NUMBER --json name,state,link
   ```
2. Read the failure logs using the run ID from the link URL (last numeric segment):
   ```bash
   gh run view <run-id> --log-failed
   ```
3. Diagnose and fix the issue
4. Re-run Step 2 (tests) locally before pushing
5. Push fixes — this counts as a new push, so **reset the monitoring loop** (go to Step 5a with a fresh 20-minute budget)

### 5b. Monitor for Reviews

Poll for reviewer comments every 2 minutes, for up to 10 cycles (20 minutes total). Check for **both** new comments **and** unresolved review threads:

```bash
# Inline review comments
gh api repos/{owner}/{repo}/pulls/$PR_NUMBER/comments --jq '.[] | "[\(.user.login)] \(.path) L\(.line // "?"): \(.body[0:300])"'
# Review summaries
gh api repos/{owner}/{repo}/pulls/$PR_NUMBER/reviews --jq '.[] | "[\(.user.login)] \(.state): \(.body[0:300])"'
# Issue-level comments
gh api repos/{owner}/{repo}/issues/$PR_NUMBER/comments --jq '.[] | "[\(.user.login)] \(.body[0:300])"'
# Unresolved threads (the authoritative signal for "are we done?")
gh api graphql -f query='{ repository(owner: "{owner}", name: "{repo}") { pullRequest(number: '$PR_NUMBER') { reviewThreads(first: 50) { nodes { isResolved } } } } }' --jq '[.data.repository.pullRequest.reviewThreads.nodes[] | select(.isResolved == false)] | length'
```

**The loop is not done until ALL of these are true:**
1. CI checks are passing (not pending, not failed)
2. Zero unresolved review threads
3. No new reviews have arrived in the last polling cycle

If all three conditions are met for two consecutive cycles, proceed. Otherwise keep polling.

### 5c. Address Review Comments

For each review comment:

1. Read the full comment to understand the feedback
2. Evaluate whether the feedback is valid and actionable
3. If valid: make the code change
4. If by-design or out-of-scope: reply with a clear explanation of why
5. React with 👀 on the comment and resolve the thread after addressing it

**Batch fixes:** Collect all comments from a review round, fix them all, then commit and push once. Each push triggers new review cycles from bots.

### 5d. Push Fixes and Re-Monitor — **RESET THE LOOP**

After addressing all comments:

1. Re-run Step 2 (tests)
2. Commit all fixes in a single commit with a message like: "Address review feedback: <summary>"
3. Push to the remote branch
4. **IMPORTANT: Every push triggers new bot review cycles.** Reset the monitoring loop timer to zero and restart from Step 5a with a fresh 20-minute budget. Do NOT carry over time from the previous loop — the push invalidates all prior "all clear" signals

---

## Step 6: Stop Conditions

The review loop stops when one of these conditions is met:

- **PR is approved** by the user — proceed to squash merge:
  ```bash
  gh pr merge $PR_NUMBER --squash --delete-branch
  ```
- **User cancels** — leave the PR open, report current status and any unaddressed comments
- **Docs-only PRs** (all changed files match `*.md`, `*.txt`, `docs/**`) — merge immediately without waiting for user approval

---

## Error Handling

| Scenario | Behaviour |
|----------|-----------|
| Temptation to stop after `gh pr create` | **Invalid.** Run Step 5 or explicitly invoke the Definition of done exception. |
| `dotnet test` fails | Stop. Fix failures. Re-run. Do not proceed. |
| `ng test --watch=false` fails | Stop. Fix failures. Re-run. Do not proceed. |
| `git push` fails | Report the error. Likely a rebase/conflict issue — prompt user. |
| PR already exists for branch | Update the existing PR body instead of creating a duplicate. |
| No OpenSpec spec found | Omit the Spec line from the PR body. Proceed normally. |
| CI fails after PR creation | Read logs, fix issues, push fixes, re-monitor. |
| Review polling timeout (20 min) | If CI is passing, zero unresolved threads, and no new reviews arrived in the last cycle — for 2 consecutive cycles — proceed. Otherwise ask the user whether to keep waiting. |
| Push after fixing review comments | **Reset the 20-minute monitoring loop to zero.** Every push triggers new bot reviews — prior "all clear" signals are invalid. |
