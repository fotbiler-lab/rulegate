## Summary

<!-- Describe the change and its intended outcome. -->

## Motivation

<!-- Explain the problem this pull request solves. -->

## Changes

<!-- List the important implementation and documentation changes. -->

## Public API impact

- [ ] No public API change
- [ ] Adds a public API
- [ ] Changes an existing public API
- [ ] Removes or deprecates a public API

<!-- Explain any checked public API impact. -->

## Authorization and security impact

- [ ] No authorization or security impact
- [ ] Changes authorization evaluation
- [ ] Changes manifest loading or compilation
- [ ] Changes ASP.NET Core integration
- [ ] Changes diagnostics, logging, or HTTP responses
- [ ] Changes packaging or release behavior

<!-- Explain the security boundaries and fail-closed behavior. -->

## Validation

<!-- List the exact validation commands and results. -->

- [ ] Formatting verification passed
- [ ] Release build passed
- [ ] Relevant focused tests passed
- [ ] Complete test suite passed
- [ ] Package-only consumer tests passed when applicable
- [ ] `git diff --check` passed

## Documentation

- [ ] README or documentation updated
- [ ] CHANGELOG updated when appropriate
- [ ] No documentation change is required

## Checklist

- [ ] The change is focused and contains no unrelated refactoring.
- [ ] New behavior has regression tests.
- [ ] Failure and malformed-input behavior is tested.
- [ ] Sensitive authorization inputs are not exposed.
- [ ] Generated or serialized output remains deterministic.
- [ ] All relevant local validations passed before opening this pull request.
- [ ] The complete consumer workflow was manually reviewed where applicable.
- [ ] The working tree and staged diff were reviewed.
