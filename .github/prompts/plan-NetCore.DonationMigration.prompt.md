## Plan: Multi-phase .NET 10 migration, SQL Server→PostgreSQL, and Aspire consolidation

**TL;DR:** Migrate the entire solution to .NET 10 with latest NuGet packages (focusing on IdentityProvider and AuthenticationDatabase upgrades from net6.0), replace SQL Server with PostgreSQL throughout, update Directory.Packages.props with consolidated versions, refactor EF Core migrations for PostgreSQL, consolidate the mixed Aspire/Docker-Compose orchestration into a single unified Aspire-based approach, and reintroduce a CQRS message pipeline via a light-weight in-house dispatcher instead of MediatR (no MediatR references remain in the codebase per scan).

### Steps

1. **Audit and prepare Directory.Packages.props** — Consolidate package versions across all projects (net10.0 for all, upgrade IdentityServer4→IdentityServer integration stack, EF Core to 10.0.1 with PostgreSQL provider instead of SQL Server, remove legacy net6.0 pins).

2. **Upgrade Identity stack to net10.0** — Retarget NetCore.Donation.IdentityProvider.csproj and NetCore.Donation.Infrastructure.AuthenticationDatabase.csproj from net6.0 to net10.0, updating IdentityServer4 dependencies.

3. **Replace SQL Server packages with PostgreSQL** — Update Directory.Packages.props (remove Microsoft.EntityFrameworkCore.SqlServer, add Npgsql.EntityFrameworkCore.PostgreSQL); update .csproj files to reference new EF Core PostgreSQL provider.

4. **Create PostgreSQL migrations** — Generate EF Core migrations for both ApplicationDatabaseContext.cs and ApplicationDbContext.cs targeting PostgreSQL, create migration service logic in NetCore.Donation.Migration.

5. **Update connection strings and DbContext factories** — Clean appsettings.*: swap SQL Server connection strings to PostgreSQL (shared names across API/Migration/IdentityProvider/AuthenticationDatabase), drop unused SQL Server settings, add Postgres pooling parameters, ensure Redis endpoints align; update DesignTimeDbContextFactory.cs for PostgreSQL.

6. **Consolidate Aspire orchestration** — Merge existing Aspire AppHost (src/client/NetCore.Donation/AppHost/Program.cs) with docker-compose services (PostgreSQL, Redis, migration, API, UI), remove duplicate docker-compose files, ensure single source of truth.

7. **Restore CQRS via self-hosted dispatcher** — Since MediatR is absent, introduce a minimal CQRS pipeline: define request/response contracts, handlers, pipeline behaviors (logging, validation, transaction boundaries), and a scoped dispatcher/service locator wired through DI; update API/Application layers to route commands/queries through this dispatcher.

8. **Align Dockerfiles and compose** — Update API/Migration/IdentityProvider Dockerfiles to target net10.0 runtime images, set Postgres env vars, and remove SQL Server tooling; update docker-compose (local/prod) to replace mssql with postgres + health checks, wire env vars to match appsettings, and mirror the same in Aspire resource definitions.

### Further Considerations

1. **Identity migration complexity** — IdentityServer4 is deprecated; recommend assessing whether to upgrade to AspNetCore.Identity + Duende IdentityServer 7+ (breaking change) vs. continuing IdentityServer4 with net10.0 (limited support). Impact on authentication flow and login service configuration? Disable IdentityServer4 projects and related projects for now. 

2. **Data migration strategy** — SQL Server → PostgreSQL requires schema translation and data export/import. Should you use EF Core migrations for schema, plus a separate data migration utility (ETL script), or prefer scripting in Aspire initialization?

3. **Aspire AppHost structure** — Should the unified Aspire host (currently in `src/client/NetCore.Donation/AppHost/`) move to solution root for clarity, or remain in client folder? How to handle PostgreSQL + Redis resource definitions and health checks?
