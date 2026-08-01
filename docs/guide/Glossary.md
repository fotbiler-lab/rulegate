# RuleGate Glossary

| Term                         | Meaning                                                                                             |
| ---------------------------- | --------------------------------------------------------------------------------------------------- |
| ABAC                         | Attribute-based access control; decisions based on typed subject, resource, or context attributes   |
| Action                       | Stable business verb being authorized, such as `read` or `approve`                                  |
| Authentication               | Establishing and validating who or what is calling                                                  |
| Authorization                | Deciding whether a subject may perform an action on a resource in the current context               |
| Authorization context        | Trusted evaluation time and request-specific attributes                                             |
| Authorization route          | Exact `resourceType` + `action` pair used to select a policy                                        |
| CBAC                         | Context-based access control; decisions based on request/session/environment circumstances          |
| Context age                  | Maximum accepted age of authentication or MFA evidence                                              |
| Decision                     | Final allowed or denied engine result with structured failures for denied outcomes                  |
| Default deny                 | Absence of a matching policy or grant results in denial                                             |
| Enrichment                   | ASP.NET Core pipeline that adds trusted subject, resource, and context attributes before evaluation |
| Fail closed                  | Missing, malformed, incompatible, cancelled, or failed input cannot become access                   |
| Frontend projection          | UI-only set of permission, policy, and role identifiers; never backend authority                    |
| Identity provider            | System that authenticates callers and issues identity/session evidence, such as Keycloak            |
| Indeterminate                | A requirement could not be evaluated safely; protected operations remain denied                     |
| Manifest                     | YAML schema that declares RuleGate policies and requirements                                        |
| MFA                          | Multi-factor authentication                                                                         |
| PBAC                         | Permission-based access control; decisions based on exact capabilities                              |
| Policy                       | Unique ID, resource type, action, and one requirement tree                                          |
| Policy source                | Local component that supplies complete policy definitions for a snapshot                            |
| RBAC                         | Role-based access control; decisions based on stable responsibilities                               |
| Requirement                  | One permission, role, attribute, time, context, custom, or logical condition                        |
| Resource                     | Business object or collection being protected, with type, optional ID, and trusted attributes       |
| Resource-based authorization | Decision using the actual protected object's identity and state                                     |
| Satisfied                    | Requirement conclusively passed                                                                     |
| Snapshot                     | Complete immutable active policy set, or UI grant projection depending on context                   |
| Subject                      | Authenticated actor with ID, roles, permissions, and trusted attributes                             |
| Trusted source               | Server-side component whose data is validated and authorized for use in a decision                  |

For the conceptual model, read
[Authorization foundations](01-Authorization-Foundations.md). For complete
contract detail, read the [authorization model](../authorization-model.md).

---

Back to [The RuleGate Guide](README.md)
