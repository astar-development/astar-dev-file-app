# Summary

Please include a brief description of the change and the motivation.

---

## TDD Checklist (required)

- [ ] I added one or more failing tests that demonstrate the desired behavior before implementing production code.
- [ ] I confirmed the new test(s) fail locally before writing production code.
- [ ] I implemented the minimal production code to make the tests pass.
- [ ] I ran the full test suite locally and all tests pass.
- [ ] The failing-test commit is included in this branch history (the failing test must be present in the PR history before or alongside production code).

If the TDD checklist is not applicable for this PR (e.g., documentation-only or chore), explain why in the description.

---

## How to run tests locally

```bash
# Restore and run tests
dotnet restore AStar.Dev.OneDrive.Sync.Client.slnx
dotnet test --verbosity normal
```

---

## Notes for reviewers

- Verify that the PR history includes the failing-test commit (or an explanation why not).
- Ensure CI passed for all OS runners.
