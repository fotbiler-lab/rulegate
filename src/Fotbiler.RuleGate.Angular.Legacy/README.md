# RuleGate Angular Legacy

`@fotbiler/rulegate-angular-legacy` provides the separately maintained Angular
12–19 adapter for applications that use NgModule, observables, class-based
route guards, and classic input directives.

Angular 9–11 applications use `@fotbiler/rulegate-client` through a small
host-owned Angular service because those releases predate the stable partial-Ivy
library format used by this adapter.

```bash
pnpm add @fotbiler/rulegate-angular-legacy@1.0.0 @fotbiler/rulegate-client@1.0.0
```

Frontend checks improve user experience only. The backend remains the security
boundary. See the
[frontend compatibility guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/frontend-compatibility.md)
for installation and support details.

The [frontend integration chapter](https://github.com/fotbiler-lab/rulegate/blob/main/docs/guide/08-Frontend-Integration.md)
connects the legacy client, class guard, NgModule directives, backend security
boundary, modern adapter, and framework-independent client in one guide.
