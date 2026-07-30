# RuleGate Reference Applications

These applications consume published RuleGate packages. They do not reference
projects under `src/`.

| Sample                                               | Demonstrates                                                                             |
| ---------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| [Minimal ASP.NET Core](aspnetcore-minimal/README.md) | A minimal runnable host with a detailed manifest catalog and Minimal API authorization   |
| [Document approval](document-approval/README.md)     | Angular, PrimeNG, Keycloak, ASP.NET Core, SQLite, enrichment, and resource authorization |

The document-approval web application is also the modern Angular reference. It
uses generated identifiers, route guards, visibility directives, disabled-state
directives, and the optional Keycloak adapter. Browser checks only shape the
experience; every protected operation is authorized again by the API.

Use its [manual verification guide](document-approval/verification.md) to
reproduce the dedicated Keycloak configuration and test the complete
permission-, role-, attribute-, context-, time-, and resource-based
authorization matrix.
