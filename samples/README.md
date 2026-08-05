# RuleGate Sample Portfolio

RuleGate samples consume published packages. They do not reference projects
under `src/`.

The portfolio contains compact technical examples, realistic domain reference
applications, and compatibility-focused applications. New samples are
repository additions and do not create a RuleGate package version by
themselves.

## Versioning rule

A sample may be added or expanded without changing the RuleGate package
version.

A semantic version is created only when the sample requires a change to a
published package, public API, runtime behavior, integration contract, or
compatibility guarantee.

Exact patch versions are pinned when implementation begins. The table below
records the intended major platform baseline and may be adjusted when a sample
enters development.

## Current runnable samples

| Sample                                               | Domain                         | Backend                     | Frontend and UI                     | Identity                                         | Data   | Authorization focus                                                                  | Status       |
| ---------------------------------------------------- | ------------------------------ | --------------------------- | ----------------------------------- | ------------------------------------------------ | ------ | ------------------------------------------------------------------------------------ | ------------ |
| [Minimal ASP.NET Core](aspnetcore-minimal/README.md) | Technical introduction         | .NET 10 Minimal API         | None                                | Sample-owned deterministic header authentication | None   | YAML, CLI, default deny, permissions, roles, resource and context rules              | ✅ Available |
| [Document approval](document-approval/README.md)     | Document and approval workflow | ASP.NET Core 10, EF Core 10 | Angular 22, PrimeNG 22, PrimeFlex 4 | Keycloak 26.x                                    | SQLite | RBAC, ABAC, CBAC, ownership, organization scope, workflow state, time and enrichment | ✅ Available |

The document-approval web application is also the modern Angular reference. It
uses generated identifiers, route guards, visibility directives,
disabled-state directives, and the optional Keycloak adapter.

Browser checks only shape the experience. Every protected operation is
authorized again by the API.

Use its [manual verification guide](document-approval/verification.md) to
reproduce the dedicated Keycloak configuration and test the complete
permission-, role-, attribute-, context-, time-, and resource-based
authorization matrix.

## Planned portfolio

| Sample project                      | Domain and scenario                      | Backend baseline            | Frontend and UI baseline           | Identity boundary                 | Data store      | Primary authorization coverage                                                               | Delivery dependency           |
| ----------------------------------- | ---------------------------------------- | --------------------------- | ---------------------------------- | --------------------------------- | --------------- | -------------------------------------------------------------------------------------------- | ----------------------------- |
| `healthcare-patient-access-mvc`     | HBYS patient-record access               | ASP.NET Core MVC 3.1        | Razor and Bootstrap 4.6            | ASP.NET Core Identity and cookies | SQL Server 2019 | Clinic scope, treatment relationship, confidential records, break-glass context and auditing | Current RuleGate 1.0 packages |
| `student-grade-publication-legacy`  | Student grade publication                | .NET 6 Web API              | Angular 12 and PrimeNG 12          | IdentityServer4 4.x boundary      | PostgreSQL 14   | Instructor-course scope, publication windows, maker-checker and legacy Angular adapter usage | Current RuleGate 1.0 packages |
| `erp-purchase-order-approval`       | ERP purchase-order approval              | .NET 10 Web API             | Angular 22 and NG-ZORRO 22         | OpenIddict 7.x                    | PostgreSQL 17   | Approval limits, department scope, budget authority and separation of duties                 | Current RuleGate 1.0 packages |
| `fintech-transaction-approval-java` | Fintech transaction approval             | Java 25 and Spring Boot 4.x | Angular 22 and Angular Material 22 | Keycloak 26.x                     | PostgreSQL 17   | Maker-checker, transaction limits, risk context, MFA age and time windows                    | Java package family `1.1.0`   |
| `ecommerce-merchant-operations-php` | E-commerce merchant and order operations | PHP 8.5 and Laravel 13      | Blade and Bootstrap 5.3            | Laravel authentication boundary   | MySQL 8.4       | Merchant ownership, refund limits, support access and tenant scope                           | PHP package family `1.2.0`    |
| `crm-record-ownership-react`        | CRM ownership and team visibility        | .NET 10 Web API             | React 19 and Material UI 7         | OpenIddict 7.x                    | PostgreSQL 17   | Record ownership, team visibility, reassignment and manager scope                            | React integration `1.3.0`     |
| `b2b-partner-portal-vue`            | B2B partner and tenant administration    | .NET 10 Web API             | Vue 3.5 and Tailwind CSS 4         | Generic OpenID Connect boundary   | PostgreSQL 17   | Tenant isolation, partner scope and delegated administration                                 | Vue integration `1.4.0`       |

## Portfolio acceptance rules

A new sample must add meaningful coverage in at least one of these areas:

- business domain;
- authorization pattern;
- application architecture;
- supported platform generation;
- identity-provider boundary;
- frontend integration style;
- UI technology;
- persistence or deployment model.

Every new sample must include:

- a clearly scoped business problem;
- a documented authentication and trust boundary;
- backend-enforced RuleGate authorization;
- realistic allow and deny scenarios;
- a manifest or documented code-first policy source;
- deterministic policy tests;
- CLI validation, lint, test, and generation commands where applicable;
- deterministic users, resources, and fixtures;
- installation and execution instructions;
- a security note identifying trusted server-side values;
- package-only build or runtime verification;
- explicit end-of-life guidance when legacy platforms are used.

Samples that repeat an existing domain, stack, and authorization model without
adding learning or compatibility value should not be added.
