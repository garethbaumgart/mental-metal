# Surgical Removal

Remove seven aggregates and all dependent code across every layer of the application. This is the foundation step — everything else in V2 builds on a cleaned-up codebase.

## Aggregates to Remove

| Aggregate | Domain Files | App Handlers | Infra | Endpoints | Frontend | Tests |
|-----------|-------------|-------------|-------|-----------|----------|-------|
| Delegation | 5 | 13 | 2 | in Program.cs | 3 pages + service + model | 2 |
| Goal | 6 | 2 | 2 | in Program.cs | 1 page + model | 1 |
| Observation | 4 | 2 | 2 | in Program.cs | 1 page + service + model | 1 |
| OneOnOne | 6 | 7 | 2 | in Program.cs | 1 page + service + model | 1 |
| Interview | 7 | 17 | 2 | InterviewEndpoints.cs | 3 pages + service + model | 2 |
| Nudge | 5 | 11 | 2 | NudgesEndpoints.cs | 6 feature files + service | 2 |
| ChatThread | 11 | 18 | 2 | in Program.cs | 1 page + 2 services + model | 2 |

## Also Remove

- **Daily Close-Out**: domain VO (`DailyCloseOutLog`), application handlers (6 files), endpoints, frontend feature (10 files)
- **My Queue**: application handlers (4 files), endpoint, frontend feature (7 files)
- **Global Chat**: application handlers (18 files), frontend page + services
- **Initiative Chat**: application handlers (8 files), frontend components
- **Briefing aggregate**: domain entity (read-only `Briefing`), repository, EF configuration — replaced by on-demand generation in V2

## Deletion Order

Process in dependency-safe order. No killed aggregate depends on another killed aggregate, so the order is primarily about compilation:

1. **Frontend first** — delete pages, services, models, routes for all killed features. App compiles with unused backend code.
2. **Web layer** — remove endpoint registrations from `Program.cs`, delete standalone endpoint files (`InterviewEndpoints.cs`, `NudgesEndpoints.cs`).
3. **Application layer** — delete all handler files, DTOs, and services for killed aggregates.
4. **Infrastructure layer** — delete repositories, EF configurations for killed aggregates.
5. **Domain layer** — delete aggregate folders, events, repository interfaces, value objects.
6. **Tests** — delete all test files for killed aggregates across domain, application, and integration test projects.
7. **OpenSpec specs** — delete or archive spec directories for killed capabilities.

## Cross-Cutting Cleanup

### BriefingFactsAssembler (CRITICAL)

`BriefingFactsAssembler.cs` depends on repositories from 4 killed aggregates:
- `IDelegationRepository`
- `IOneOnOneRepository`
- `IObservationRepository`
- `IGoalRepository`

**Action**: Delete `BriefingFactsAssembler` entirely. V2 briefing generates directly from captures — see `daily-brief` and `weekly-brief` specs.

### BriefingService

References `BriefingFactsAssembler` and the killed `Briefing` aggregate.

**Action**: Rewrite to V2 shape (direct capture synthesis). This is covered by the `daily-brief` and `weekly-brief` specs, not this removal spec.

### Dashboard Widgets

Several widgets reference killed data:
- Overdue Summary widget references Delegations
- Today's One-on-Ones widget references OneOnOnes
- Top of Queue widget references MyQueue (which references multiple killed aggregates)

**Action**: Remove these widgets. V2 dashboard is covered by `dashboard-v2` spec.

### MentalMetalDbContext

Contains `DbSet<>` properties for all killed aggregates.

**Action**: Remove DbSet properties, remove `OnModelCreating` configuration calls for killed entities.

### DependencyInjection.cs

Registers repositories and services for killed aggregates.

**Action**: Remove all registrations for killed aggregate repositories and services.

### Program.cs

Maps endpoints for killed aggregates.

**Action**: Remove all `Map*Endpoints()` calls for killed features.

### app.routes.ts

Contains route definitions for killed pages.

**Action**: Remove route entries for delegations, goals, observations, one-on-ones, interviews, global-chat, nudges, my-queue.

### sidebar.component.ts

Contains navigation links to killed pages.

**Action**: Remove navigation entries for killed features.

## Database Migration

Create a single forward migration: `V2_RemoveKilledAggregates`

### Tables to Drop

```
delegations
goals
goal_check_ins
observations
one_on_ones
action_items          (child of OneOnOne)
follow_ups            (child of OneOnOne)
interviews
interview_scorecards  (child of Interview)
nudges
nudge_cadences        (child of Nudge)
chat_threads
chat_messages         (child of ChatThread)
context_scopes        (child of ChatThread)
source_references     (child of ChatThread)
briefings
pending_brief_updates (child of Initiative Living Brief)
```

### Columns to Drop

- `users.daily_close_out_log` (or equivalent JSON column)

### Foreign Keys

Drop any foreign keys from killed tables that reference kept tables (e.g., delegation → person, delegation → initiative) before dropping the killed tables.

### Migration Direction

Forward only. No `Down()` method — this is a one-way pivot. Data in killed tables is development/test data only.

## Acceptance Criteria

- [ ] All 7 killed aggregate folders deleted from Domain layer
- [ ] All killed handler files deleted from Application layer
- [ ] All killed repositories and EF configurations deleted from Infrastructure layer
- [ ] All killed endpoint registrations removed from Web layer
- [ ] All killed pages, services, models, and routes removed from Frontend
- [ ] All killed test files removed from test projects
- [ ] `MentalMetalDbContext` compiles without killed DbSets
- [ ] `DependencyInjection.cs` compiles without killed registrations
- [ ] `Program.cs` compiles without killed endpoint mappings
- [ ] `app.routes.ts` compiles without killed routes
- [ ] Migration runs successfully and drops all killed tables
- [ ] `dotnet build src/MentalMetal.slnx` compiles with zero errors
- [ ] `npx ng build` compiles with zero errors
- [ ] `dotnet test src/MentalMetal.slnx` passes (remaining tests only)
- [ ] No references to killed aggregate types remain in kept code (grep verification)
