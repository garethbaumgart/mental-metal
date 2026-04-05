# Mental Metal

An AI-powered command center for engineering managers who are too busy to stay organized.

## The Problem

Your information is scattered across your head, Google Docs, Confluence, Jira, Google Chat, and meeting transcripts. By Friday you've forgotten what you promised on Monday. Performance review time is a scramble. You don't know what you've delegated or what's overdue. You've tried tools before but you're too busy to maintain them.

**Mental Metal fixes this.** It passively accumulates context from your day -- meeting transcripts, quick notes, ad-hoc captures -- and uses AI to build a living, queryable picture of your people, projects, and priorities. You capture raw; it organizes, links, and surfaces what matters.

### What it does

- **Quick Capture** -- dump raw thoughts with zero friction; AI links them to the right people, initiatives, and commitments
- **Transcript & Audio Processing** -- paste transcripts or record meetings; AI extracts action items, decisions, commitments, and risks
- **Initiative Living Briefs** -- AI-maintained single source of truth for each project: status, decisions, risks, dependencies
- **People Lens** -- accumulated 1:1 history, observations, goals, and delegations per direct report; quarterly reviews write themselves
- **Bidirectional Commitments** -- track what you owe and what's owed to you, with overdue nudges
- **Daily Briefing & Queue** -- your morning command center with prioritized actions across everything
- **AI Chat** -- ask natural language questions across all your data: "What did I promise in last week's leadership meeting?"

### What it is NOT

A project management tool, calendar app, communication tool, or note-taking app. It's the leadership overlay that tells you where things actually stand.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular 21, PrimeNG 21, Tailwind CSS v4 |
| Backend | .NET 10 Minimal APIs |
| Database | PostgreSQL |
| Architecture | Clean Architecture, DDD, CQRS |
| Testing | xUnit (.NET), Vitest (Angular) |
| CI | GitHub Actions |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) with npm
- [Docker](https://www.docker.com/) (provides PostgreSQL via docker-compose)

### Setup

```bash
# Clone the repo
git clone https://github.com/garethbaumgart/mental-metal.git
cd mental-metal

# Backend
cd src/MentalMetal.Web
dotnet restore
dotnet build

# Frontend
cd ClientApp
npm install
```

### Run

```bash
# Start the full dev stack (PostgreSQL + API + Angular with hot reload)
docker compose --profile dev-stack up
```

The app is available at `http://localhost:4200`. The API serves on `http://localhost:5002`.

### Test

```bash
dotnet test src/MentalMetal.slnx                                    # .NET tests
cd src/MentalMetal.Web/ClientApp && npx ng test --watch=false        # Angular tests
```

## Project Structure

```text
src/
  MentalMetal.Domain/          # Aggregates, value objects, domain events
  MentalMetal.Application/     # Commands, queries, DTOs, domain services
  MentalMetal.Infrastructure/  # EF Core, repositories, external integrations
  MentalMetal.Web/             # Minimal API endpoints + Angular SPA
    ClientApp/                 # Angular 21 app
design/                        # Domain model, relationships, spec plan, OpenSpec config
docs/                          # Product brief
```

## Contributing

### Feature Development with OpenSpec

All product features are built using [OpenSpec](https://github.com/anthropics/openspec), a spec-driven development workflow. Scaffolding and infrastructure work lives outside this process, but every user-facing feature follows the full cycle:

```text
openspec propose <feature>     # Write a proposal with motivation and scope
openspec design <feature>      # Flesh out the design, API contracts, and domain changes
openspec tasks <feature>       # Break into implementable tasks
openspec spec <feature>        # Define BDD scenarios that become acceptance tests
       ↓
  Build on a feature branch
       ↓
  PR → CI → Review → Merge
```

A feature is "done" when:

- The OpenSpec spec exists with BDD scenarios
- All scenarios pass as tests
- The PR is reviewed and merged
- Exploratory testing passes

### Guidelines

1. **Branch per feature** -- one branch per OpenSpec spec
2. **One aggregate at a time** -- never have two people working in the same aggregate simultaneously
3. **Specs define the contract** -- frontend can build against a spec without waiting for backend; downstream devs can mock until upstream merges
4. **Domain model decisions are discussed** -- check `design/domain-model.md` and `design/spec-plan.md` before making changes to aggregate boundaries
5. **Test-first** -- BDD scenarios from specs become tests
6. **Keep it consistent** -- follow existing patterns in the codebase

### Key Design Documents

- [`docs/product-brief.md`](docs/product-brief.md) -- vision, features, success criteria
- [`design/domain-model.md`](design/domain-model.md) -- DDD aggregates and relationships
- [`design/spec-plan.md`](design/spec-plan.md) -- feature tiers and dependencies

## License

[MIT](LICENSE) -- Copyright (c) 2026 Gareth Baumgart
