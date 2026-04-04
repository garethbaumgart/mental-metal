# CLAUDE.md — Mental Metal

## Critical Rules / Banned Patterns

### Angular syntax
- **BANNED:** `*ngIf`, `*ngFor`, `*ngSwitch`, `[ngClass]`
- **REQUIRED:** `@if`, `@for`, `@switch`, `[class.x]="expr"` control flow

### Zoneless change detection (Angular 21)
- Plain properties do NOT trigger change detection — must use signals
- Never mutate signal values directly — always create new references via `.update()` or `.set()`

### Theming — colours
- **PrimeNG is the single source of truth for all colours.** Defined via `definePreset(Aura, {...})` in `app.config.ts`, consumed as CSS variables (`--p-primary-color`, `--p-surface-*`, etc.)
- **`tailwindcss-primeui` bridges PrimeNG tokens to Tailwind utilities.** Use `bg-primary`, `text-primary`, `bg-surface-50`, `text-muted-color`, etc.
- **NEVER** use hardcoded Tailwind colour utilities (`bg-gray-100`, `text-violet-600`, etc.)
- **NEVER** define custom `--color-*` CSS variables that duplicate PrimeNG tokens
- **NEVER** use `dark:` Tailwind prefix — dark mode is handled by `.dark` class on `<html>`, which PrimeNG and `tailwindcss-primeui` respond to automatically
- **Tailwind is for layout/spacing/responsive ONLY** — `flex`, `grid`, `gap-*`, `p-*`, `m-*`, `w-*`, `h-*`, `justify-*`, `items-*`, `sm:`, `lg:`, `hidden`, `sr-only`, `truncate`, etc.
- **CSS layer order:** `base < primeng < utilities` — enforced via PrimeNG `cssLayer` config

### Component CSS colours
- Use PrimeNG design token variables: `var(--p-primary-color)`, `var(--p-surface-100)`, etc.
- Never use hardcoded hex/rgba values

### Boy Scout Rule (scoped)
- Fix banned patterns in lines you're already changing, but do NOT expand scope to unrelated code
- No separate commits for cleanup — fold into the feature commit

---

## Architecture

- **Domain-Driven Design** with rich domain models (behaviour lives on aggregates, not in services)
- **Vertical Slice Architecture** — feature folders, not layer folders
- **CQRS** — Command Query Responsibility Segregation
- **Clean Architecture Layers:** Domain → Application → Infrastructure → Web
- **Project references:** Domain has zero external dependencies. Application references Domain. Infrastructure references Application. Web references Infrastructure. Dependencies only point inward.
- **Aggregate boundaries:** Enforce invariants at the aggregate root. No cross-aggregate references by entity — use IDs only. Value objects are embedded within aggregates.
- **Repository interfaces** live in Domain; implementations live in Infrastructure
- **All entities are user-scoped** — UserId is the multi-tenancy boundary

---

## Backend Conventions (.NET 10)

- **Minimal APIs** — no controllers
- **Entity Framework Core** with PostgreSQL (Npgsql provider)
- **EF Core LINQ pitfalls:** `HashSet.Contains()` and `.ToLowerInvariant()` are NOT translatable to SQL — use `List<T>.Contains()` and `.ToLower()` instead
- **Vertical slice handlers** — one file per use case (e.g., `CreatePerson.cs`, `GetUserInitiatives.cs`)
- **DTOs/Response models** — never expose domain entities directly through the API
- **Migration rule:** Migrations merged to main are immutable. Edit freely on local branches or in PRs, but once merged, create a new migration instead.

---

## Frontend Conventions (Angular 21)

- **Standalone components only** — no NgModules
- **Signals for all state** — zoneless change detection (Zone.js not included)
- **DI via `inject()`** — not constructor injection
- **Component decision hierarchy:** (1) Check PrimeNG first, (2) Tailwind layout utilities, (3) Custom CSS as last resort
- **Signal patterns:** `readonly` on all signal members, `signal()`, `computed()`, `input.required()`, `output()`
- **Signal Forms** (Angular 21) — preferred over Reactive Forms for new forms

---

## Theming Quick Reference

```
SOURCE OF TRUTH:    PrimeNG definePreset(Aura, {...}) in app.config.ts
BRIDGE TO TAILWIND: tailwindcss-primeui plugin in styles.css
DARK MODE:          .dark class on <html> — toggled by ThemeService
LAYER ORDER:        base < primeng < utilities
TAILWIND ROLE:      Layout, spacing, responsive ONLY — never colour

ALLOWED:            bg-primary, text-surface-50, var(--p-primary-color)
BANNED:             bg-gray-100, text-violet-600, dark:bg-gray-900, --color-bg-base
```

---

## Dev Commands

```bash
# Run backend tests
dotnet test src/MentalMetal.slnx

# Run frontend tests
(cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false)

# Run E2E tests (requires Docker stack)
docker compose --profile dev-stack up -d --wait
(cd tests/MentalMetal.E2E.Tests && npm test)
docker compose --profile dev-stack down

# Start only the dev database (for native dev)
docker compose --profile dev up -d

# Start full Docker dev stack
docker compose --profile dev-stack up

# Database migrations
cd src/MentalMetal.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../MentalMetal.Web
dotnet ef migrations remove --startup-project ../MentalMetal.Web  # local only
```
