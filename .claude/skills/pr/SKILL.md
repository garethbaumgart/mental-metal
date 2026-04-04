---
name: pr
description: Create a PR, run tests, self-review, monitor CI and review feedback, then merge when approved.
---

# /pr — Pull Request Skill

Automates the full PR lifecycle: branch verification, PR creation, pre-flight tests, self code review, review monitoring, and merge.

> **Note:** This skill is scoped to the Mental Metal repository (.NET backend, Angular frontend when present). Frontend steps are conditional — they are skipped automatically when the Angular project does not yet exist.

---

## Step 1: Verify Branch and Uncommitted Changes

1. Run `git branch --show-current` to check the current branch.
2. If on `main`, **stop and ask the user** for a branch name. Create a feature branch with one of these prefixes: `feat/`, `fix/`, `chore/`, `docs/`.
3. Check for uncommitted changes with `git status`. If any exist, stage and commit them with a descriptive message before proceeding. Stage specific files — never use `git add .` or `git add -A`.

---

## Step 2: Create the PR

Get the PR up early so CI and review bots start working in parallel.

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
   If no PR exists, create one in step 5 and capture the number afterwards:
   ```bash
   PR_NUMBER="$(gh pr view --json number -q '.number')"
   ```
4. If `openspec/specs/` exists, look for a spec related to this branch. If the directory does not exist, skip this step.
5. Create the PR using this template:

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

---

## Step 3: Pre-Flight — Run Tests

Run available test suites and **abort if any fail**:

1. **Backend tests** (always run):
   ```bash
   dotnet test src/MentalMetal.slnx
   ```

2. **Frontend tests** (only if `src/MentalMetal.Web/ClientApp/angular.json` exists):
   ```bash
   (cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)
   ```
   If the ClientApp directory does not exist, skip this step.

**STOP if any test suite fails.** Diagnose and fix the failures, then re-run. Do not proceed past this step with failing tests.

After fixing, commit and push the fixes.

---

## Step 4: Self Code Review

Review the full diff against main before proceeding:

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

Apply all fixes immediately. Commit and push before proceeding.

---

## Step 5: Monitor CI and Review Comments (Review Loop)

After pushing, actively monitor and address feedback.

### 5a. Wait for CI

Poll CI status until all checks complete:

```bash
gh pr checks $PR_NUMBER
```

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
4. Re-run Step 3 (tests) locally before pushing
5. Push fixes and restart the monitoring loop

### 5b. Monitor for Reviews

Poll for reviewer comments every 2 minutes, for up to 5 cycles (10 minutes total). If all comments have been addressed after a cycle, stop polling and proceed:

```bash
gh api repos/{owner}/{repo}/pulls/$PR_NUMBER/comments --jq '.[] | "[\(.user.login)] \(.path) L\(.line // "?"): \(.body[0:300])"'
gh api repos/{owner}/{repo}/pulls/$PR_NUMBER/reviews --jq '.[] | "[\(.user.login)] \(.state): \(.body[0:300])"'
gh api repos/{owner}/{repo}/issues/$PR_NUMBER/comments --jq '.[] | "[\(.user.login)] \(.body[0:300])"'
```

### 5c. Address Review Comments

For each review comment:

1. Read the full comment to understand the feedback
2. Evaluate whether the feedback is valid and actionable
3. If valid: make the code change
4. If by-design or out-of-scope: reply with a clear explanation of why

**Batch fixes:** Collect all comments from a review round, fix them all, then commit and push once. Each push triggers new review cycles from bots.

### 5d. Push Fixes and Re-Monitor

After addressing all comments:

1. Re-run Step 3 (tests)
2. Commit all fixes in a single commit with a message like: "Address review feedback: <summary>"
3. Push to the remote branch
4. Restart the review monitoring loop from Step 5a — wait for reviewers to review the latest commit

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
| `dotnet test` fails | Stop. Fix failures. Re-run. Do not proceed. |
| `ng test --watch=false` fails | Stop. Fix failures. Re-run. Do not proceed. |
| `git push` fails | Report the error. Likely a rebase/conflict issue — prompt user. |
| PR already exists for branch | Update the existing PR body instead of creating a duplicate. |
| No OpenSpec spec found | Omit the Spec line from the PR body. Proceed normally. |
| CI fails after PR creation | Read logs, fix issues, push fixes, re-monitor. |
| Review polling timeout (10 min) | If no unaddressed comments remain, proceed. Otherwise ask the user whether to keep waiting. |
