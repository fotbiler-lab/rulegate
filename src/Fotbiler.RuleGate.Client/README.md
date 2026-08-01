# RuleGate Client

`@fotbiler/rulegate-client` is the framework-independent, fail-closed frontend
authorization state used by RuleGate adapters.

It projects permissions, policies, and roles supplied by a trusted application
endpoint. It improves frontend user experience only; backend authorization
remains the security boundary.

## Install

```bash
pnpm add @fotbiler/rulegate-client@1.0.0
```

Read the [frontend integration chapter](https://github.com/fotbiler-lab/rulegate/blob/main/docs/guide/08-Frontend-Integration.md)
for the connected snapshot, trust-boundary, Angular 9–11, modern, and legacy
examples. See the [frontend compatibility reference](https://github.com/fotbiler-lab/rulegate/blob/main/docs/frontend-compatibility.md)
for support levels and package-only verification details.
