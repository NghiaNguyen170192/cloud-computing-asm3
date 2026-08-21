# NetCore.Donation — Continuation Context

Last updated: 2026-08-16  
Purpose: archive of goals, progress, and conventions so work can resume later without re-deriving context from chat history.

Diagrams for the report: [`DATA_FLOW.md`](DATA_FLOW.md)

Related chats:

- [Journal receipt storage](a49fabf7-2f24-461e-9b31-b677793ceab0)
- [Transactional outbox + donation CQRS](b0fac9fa-8e7f-4607-a936-036b2d227149)

Do not revive `src/NetCore.Donation.Application/Messaging/Dispatcher.cs`. MediatR is the dispatcher.

---

## Snapshot — where things stand

| Item | Status |
|---|---|
| Pass 1 (scaffold + donation CQRS spine) | Done (older commit on `donation-implementation`) |
| Pass 2 (Journal + preferences + receipt PDF/MinIO) | Done |
| PATCH contact preferences | Done |
| Transactional outbox | **Done in working tree** — capture in `SaveChanges`, `ProcessOutboxMessagesCommand`, hosted poller |
| Donate CQRS pipeline | **Done in working tree** — command → event → next command |
| One-time vs recurring on same command | **Done in working tree** — `IsRecurring` + `RecurringInterval.OneOff` |
| Donor UI uses `POST /api/v1/donations` | **Done in working tree** |
| Recurring *future* transactions | **Out of scope** — one schedule + one transaction now |
| Separate Journal/Ledger DB | **Out of scope** — same PostgreSQL |
| Tests last green | Domain 30, Application 49, Infra DB 37, Api 5 |
| Live Aspire smoke | Still deferred if Docker/WSL is broken |
| Commit | User will update/commit later today — **do not commit unless asked** |

**Immediate resume actions:**

1. Apply pending EF migrations if the local DB is behind: `AddOutboxMessages`, `AddTransactionStatus` (`OneOff` is a string enum value — no extra migration).
2. Aspire smoke: donate form (one-time and recurring) → `POST /api/v1/donations` → wait for outbox poller (5s) → transaction + optional receipt/journal → My gifts / admin.
3. Commit when asked. Keep `Dispatcher.cs` deleted.

---

## What was implemented this session (handoff)

### Transactional outbox

- `SaveChanges` / `SaveChangesAsync` **does not** MediatR-publish before commit.
- Pending `Entity.DomainEvents` become `OutboxMessages` in the same transaction, then events are cleared.
- Stamps `CorrelationId` (`X-Correlation-ID`) and `IdempotencyKey` (`X-Idempotency-Key`). If no idempotency header, correlation is copied into both columns.
- Dedupe: skip insert when `IdempotencyKey + MessageType` already exists.
- Deleted/detached entities: clear domain events, do **not** enqueue outbox rows.
- Table: `OutboxMessages` (migration `20260816092518_AddOutboxMessages`).
- Processor: `ProcessOutboxMessagesCommand` — stub `IIntegrationEventPublisher` (`RecordingIntegrationEventPublisher`), then deserialize `AssemblyQualifiedName` + MediatR `IPublisher.Publish`.
- API hosted `OutboxProcessor` polls every 5s. **API tests must `services.RemoveAll<IHostedService>()`** or the poller hangs WebApplicationFactory tests.

### Donate pipeline (CQRS POC)

```text
COMMAND → Handler → Domain → DOMAIN EVENT → Outbox → MediatR → Next COMMAND
```

| Design name | Actual |
|---|---|
| React / Vue / Astro | Blazor Server `NetCore.Donation.UI` `:6010` |
| Donation aggregate | **No entity.** `PaymentSchedule` is the donation intent (`RaiseDonationCreated`) |
| `DonationPaymentMethod` | Existing `PaymentMethod` |
| Journal / Ledger DB | Same `ApplicationDatabaseContext` |
| `IsAnonymous`, `DonorMessage` | Not stored. Preferences: `DoNotEmail` / `DoNotSms` |
| Event type names | `*DomainEvent` suffix, Country-style |

**HTTP entry:** `POST /api/v1/donations` → `UserMakesDonationCommand`  
**Result:** `ContactId`, `PaymentMethodId`, `PaymentScheduleId`, `IsRecurring` (transaction is **not** created in this request).

**Commands** (one type per file under `Application/Donation/`):

- `UserMakesDonationCommand` / Handler
- `ProcessDonationTransactionCommand` / Handler — `Transaction.CreatePending`
- `CompleteDonationTransactionCommand` / Handler — `IDonationTransactionOutcome` (API: 50/50 `RandomDonationTransactionOutcome`)
- `GenerateDonationReceiptCommand` / Handler — PDF via `ReceiptDocumentService` (after DB commit is still an accepted limitation)
- `CreateJournalEntryCommand` / Handler — saga path; REST `CreateJournalCommand` still exists

**Events** (Domain `Events/` + Application `Donation/Events/` handlers):

- `DonationCreated` → **sends** `ProcessDonationTransactionCommand`
- `ContactCreated`, `DonationPaymentMethodCreated` → log only
- `TransactionPending` → **sends** `CompleteDonationTransactionCommand`
- `TransactionSucceeded` → **parallel** `GenerateDonationReceiptCommand` + `CreateJournalEntryCommand` via `IServiceScopeFactory` (do not `Task.WhenAll` on the same DbContext)
- `TransactionFailed` → log only (no receipt, no journal)
- `DonationReceiptGenerated`, `JournalEntryCreated` → log only (terminal)

**One-time vs recurring (same command):**

- `IsRecurring: false` → persist `RecurringInterval.OneOff`
- `IsRecurring: true` → require a real interval (not `OneOff`)
- Carried on `DonationCreated` and `ProcessDonationTransactionCommand` / `TransactionPending`
- Recurring **does not** create extra transactions. `FindByPaymentScheduleIdAsync` returning an existing row is the POC idempotent skip.

**Transaction status** (migration `20260816110934_AddTransactionStatus`, existing rows default `Succeeded`):

- Pipeline: `Pending` → `Succeeded` | `Failed`
- REST `Transaction.Create` stays `Succeeded` (no pending event)

**UI**

- Donate form: one-time / recurring radio; interval dropdown only if recurring; `DonationApiClient.MakeDonationAsync`
- Thank-you: schedule id + gift type; receipt PDF only if the poller already finished
- Existing six HTTP routes unchanged for admin

---

## Project intent

Cloud-based donation management for non-profits (RMIT COSC29800 Assignment 3).

**Product flow (target):** donors submit donation requests (no real payments) → events processed asynchronously → transactions recorded → journals/receipts generated → notifications by preferred channel (email/SMS).

**Proposal AWS map (rubric-aligned — see [`AWS_RUBRIC_ALIGNMENT.md`](AWS_RUBRIC_ALIGNMENT.md)):**

| Concern | Service (rubric pts) | Local status |
|---|---|---|
| Data | **RDS PostgreSQL** (3) | Aspire Postgres |
| API | **API Gateway** (6) + **Lambda** (6) | Local Kestrel API |
| Async processing | **Lambda** outbox worker (same Lambda type); keep `OutboxMessages` | In-process poller 5s |
| Receipts storage | **S3** (3) via existing `IReceiptDocumentStorage` | MinIO + `AWSSDK.S3` |
| UI hosting | **Elastic Beanstalk** (6) + optional **CloudFront** (3) | Blazor donor `:6010` + admin `:6020` |
| Cache | **ElastiCache Redis** (3) if time | Aspire Redis |
| Analytics | **Athena** (3) if time | Admin grids only |
| Notifications | SES + SNS | Pref flags only — **usually 0 rubric pts** (still useful in demo) |
| Observability | CloudWatch | OpenTelemetry locally — support only, not a mark driver |

**Mark target:** Gateway + Lambda + EB + RDS + S3 (+ CloudFront / ElastiCache / Athena) ≥ **25** capped. Avoid double-counting EC2/S3 that only exist *inside* Beanstalk.

**Template base:** `personal/NetCore` (.NET 10 Clean Architecture + Aspire).  
**Code location:** `d:\source\RMIT\master-of-ai-new\2026-semester-02\NetCoreDonation` with namespaces `NetCore.Donation.*`.

---

## Git

| Item | Value |
|---|---|
| Repo path | `d:\source\RMIT\master-of-ai-new\2026-semester-02\NetCoreDonation` |
| Remote | `https://github.com/NghiaNguyen170192/NetCore.git` (confirm if this checkout still tracks it) |
| Branch | `donation-implementation` |
| Uncommitted | Outbox, donate pipeline, UI donate POST, docs — **commit only when asked** |
| Keep deleted | `Application/Messaging/Dispatcher.cs` |

---

## Key paths (current pipeline)

| Area | Path |
|---|---|
| Donate command | `Application/Donation/UserMakesDonation/` |
| Process / complete txn | `Application/Donation/ProcessDonationTransaction/`, `CompleteDonationTransaction/` |
| Receipt + journal saga | `Application/Donation/GenerateDonationReceipt/`, `CreateJournalEntry/` |
| Event handlers | `Application/Donation/Events/` |
| Domain events | `Domain/Events/*DomainEvent.cs` |
| PaymentSchedule / Transaction | `Domain/Entities/PaymentSchedule.cs`, `Transaction.cs` |
| Outbox capture | `Infrastructure.Database/ApplicationDatabaseContext.cs` |
| Outbox processor | `Application/Outbox/Process/`, `Api/OutboxProcessor.cs` |
| Trace query | `GET /api/v1/outbox-messages` |
| HTTP donate | `Api/Controllers/DonationController.cs` |
| Donor UI | `client/NetCore.Donation.UI/Pages/Donate.razor`, `ThankYou.razor` |
| API client | `client/NetCore.Donation.WebClient/DonationApiClient.cs` (`MakeDonationAsync`) |
| Migrations | `20260816092518_AddOutboxMessages`, `20260816110934_AddTransactionStatus` |
| Pipeline tests | `test/.../Donation/DonationCommandPipelineTest.cs` |

Object key format: `receipts/{receiptId:N}.pdf`.

---

## Local run

```bash
dotnet run --project src/client/NetCore.Donation.AppHost
```

| Endpoint | URL |
|---|---|
| Aspire Dashboard | URL printed by AppHost |
| API | `http://localhost:6000` / `https://localhost:6001` |
| Donor UI | `http://localhost:6010` / `https://localhost:6011` |
| Admin UI | `http://localhost:6020` |
| MinIO API / Console | `9000` / `9001` (`minioadmin` / `minioadmin`) |
| Swagger | `https://localhost:6001/swagger` |

Routes: `/api/v1/countries|contacts|payment-methods|payment-schedules|transactions|journals|receipts|donations|outbox-messages`  
Donate body (camelCase): `UserMakesDonationCommand` including `isRecurring`, `recurringInterval`.  
Consent: `PATCH /api/v1/contacts/{id}/preferences` body `{ id, doNotEmail, doNotSms }`.

---

## Decisions locked (do not reopen casually)

| Decision | Choice |
|---|---|
| Naming | `NetCore.Donation.*`; Country-style one type per file |
| Dispatcher | MediatR only — no `Dispatcher.cs` |
| Controllers | Explicit action bodies |
| Query DTOs | kebab-case `[JsonPropertyName]`; CUD camelCase |
| Application layer | No EF |
| Donate entry | `POST /api/v1/donations` + outbox; not six UI writes |
| Recurring | Same command; one transaction now; no scheduler |
| Journal DB | Same Postgres |
| Succeeded fan-out | Two commands, **new scopes**, not nested writes in the event handler |
| Receipt PDF | Blank PDF; generate after/around save is an accepted POC limitation |
| Storage | One S3-compatible impl for MinIO local + AWS later |

---

## Goals still pending

### Near-term

- [ ] Commit working tree when asked
- [ ] Live Aspire smoke (donate one-time + recurring, outbox drain, receipt PDF, admin lists)
- [ ] Replace blank PDF with a real document-merge template
- [ ] Expand PaymentMethod fields
- [ ] Use `DoNotEmail` / `DoNotSms` in SES/SNS
- [ ] JSON string enums (`JsonStringEnumConverter`)
- [ ] Recurring **generator** (later transactions on a schedule) — explicitly later

### Cloud / assignment

Follow [`AWS_RUBRIC_ALIGNMENT.md`](AWS_RUBRIC_ALIGNMENT.md) order:

- [ ] Real **S3** receipts (drop MinIO ServiceUrl / ForcePathStyle; IAM + region)
- [ ] **RDS** PostgreSQL + migrations/seed
- [ ] **API Gateway + Lambda** for API surface
- [ ] **Lambda** outbox worker (replace in-process poller; keep outbox table)
- [ ] **Elastic Beanstalk** for Blazor donor + admin
- [ ] **CloudFront** in front of UI (if time)
- [ ] **ElastiCache** Redis (if time)
- [ ] **Athena** analytics export/query (if time)
- [ ] SES/SNS notify using `DoNotEmail` / `DoNotSms` (demo polish; not mark driver)
- [ ] Solution architecture document + Week 12 demo from `ASSESSMENT_3_S2-1.pdf`

---

## Architecture conventions (must keep)

- Mirror `personal/NetCore` Country folder layout.
- Handlers depend on Domain ports only — never AWS SDK types in Application.
- Prefer Aspire-injected connection strings when present.
- Event handlers that start the next write **`IMediator.Send` a command**; they must not perform the next aggregate write themselves.
- `DonationCreated` is the only first-wave event that starts payment processing (not `ContactCreated` / `DonationPaymentMethodCreated`).
- Namespace collision in Application: use `Domain.Entities.Contact.Create` (folder `Contact.Create` hides the type).

---

## Known quirks

1. Enum JSON is still numeric unless a converter is registered. `RecurringInterval.OneOff = 6` in the WebClient enum.
2. Query DTOs use kebab-case `[JsonPropertyName]`.
3. Receipt GET needs `Accept: application/pdf` for binary download.
4. Hosted outbox poller has no HTTP context → new correlation guid per `SaveChanges` / poll. Unique outbox index is `IdempotencyKey + MessageType`.
5. `IdempotencyBehavior` applies to all MediatR requests including `ProcessOutboxMessagesCommand` (poller fills `IdempotencyLogs` every 5s).
6. `ApiWebApplicationFactory` must remove hosted services.
7. `ICountryRepository.FindByIdAsync` returns `Country` but can be null at runtime.
8. Processor `SaveChanges` after publish will capture **new** domain events from in-scope command handlers onto the outbox (next poll processes them). Parallel receipt/journal use **new scopes** on purpose.
9. Thank-you page may not have a receipt yet; My gifts after ~5–15s is expected.
10. Aspire Postgres password / volume and DLL-lock notes from earlier sessions still apply.

---

## Suggested next session order

1. User updates/commits the working tree (outbox + pipeline + UI + these docs).
2. After Docker/WSL: Aspire + donate both gift types + confirm outbox → transaction → journal/receipt.
3. Real receipt PDF template if needed for the assignment demo.
4. AWS packaging on the existing MediatR + outbox surface (swap poller for a queue later).
