# Api layer — rules

The HTTP entry point. **Thin.** It wires things together and translates between
HTTP and use cases — it contains no business logic.

Belongs here:
- Endpoint definitions (controllers or minimal APIs) that map to **business
  actions**, not CRUD: `POST /requests/{id}/approve-manager`,
  `POST /requests/{id}/fulfill`, etc.
- DI composition root (register Application + Infrastructure services).
- Authentication (JWT/OIDC via Keycloak) and **authorization enforcement**:
  role-based + policy-based. E.g. only Manager can approve their department's
  requests; Auditor is read-only.
- Request/response DTO mapping, model validation wiring, Swagger/OpenAPI,
  health checks, structured logging, correlation IDs.
- Global exception handling: translate domain exceptions (invalid transition,
  not-found, unauthorized) into proper HTTP status codes.

**Forbidden here:**
- No business rules or workflow logic — delegate to an Application handler.
- No direct EF Core / `DbContext` usage — go through Application ports.
- Don't expose EF entities directly; return DTOs.

## Conventions for CompanyOps

- An endpoint should: authenticate → authorize → bind+validate DTO → dispatch the
  command/query → map result to HTTP. If a method is doing more, it's misplaced.
- Authorization policies are defined here but reflect the domain role model
  (Employee, Manager, Finance, IT Admin, Auditor).
- Never return raw exceptions/stack traces to clients.
