# Data flow — browser to API to AWS

Hope and Help is a simulated donation system. Auth is not enabled. Email and SMS are logged only (no SES/SNS). There is no `Donation` entity: recurring intent is a `PaymentSchedule`; a one-off gift is a `Transaction` with no schedule.

Code types keep the Country-style `*DomainEvent` suffix (`TransactionCompletedDomainEvent`). Diagrams use the short names (`TransactionCompleted`).

Local development (Aspire) uses PostgreSQL, Redis, and MinIO. AWS Academy (COSC29800 A3) uses API Gateway + Lambda, RDS PostgreSQL, S3, and Elastic Beanstalk. The CQRS pipeline and tables are the same in both environments.

## 1. Website → C# API → persistence

Browsers talk to Blazor Server. Each site calls the API over HTTP (`DonationApiClient`). Donation rows go to PostgreSQL. Receipt PDF bytes go to object storage; receipt metadata stays in `Receipts`. Redis is beside the write path (cache), not the system of record.

```mermaid
flowchart LR
  Browser["Browser"]

  subgraph Websites["Websites — Blazor Server"]
    Donor["Hope & Help donor UI<br/>NetCore.Donation.UI"]
    Admin["Donation admin UI<br/>NetCore.Donation.Admin"]
  end

  subgraph ApiHost["C# API — NetCore.Donation.Api"]
    Ctrl["Controllers /api/v1"]
    MediatR["MediatR CQRS"]
    Handlers["Command / query handlers"]
    Repos["EF repositories"]
    Pdf["QuestPDF receipt generator"]
  end

  Postgres[("PostgreSQL / RDS<br/>Contacts, PaymentMethods,<br/>PaymentSchedules, Transactions,<br/>Journals, Receipts, OutboxMessages")]
  ObjectStore[("MinIO local / S3 AWS<br/>receipt PDFs")]
  Redis[("Redis<br/>cache")]

  Browser -->|"HTTP / SignalR"| Donor
  Browser -->|"HTTP / SignalR"| Admin
  Donor -->|"HTTP JSON<br/>DonationApiClient"| Ctrl
  Admin -->|"HTTP JSON<br/>DonationApiClient"| Ctrl
  Ctrl --> MediatR --> Handlers --> Repos
  Repos --> Postgres
  Handlers --> Pdf --> ObjectStore
  Repos -.-> Redis
```

### AWS hosting

```mermaid
flowchart TB
  DonorBrowser["Donor browser"]
  AdminBrowser["Admin browser"]

  subgraph EB["Elastic Beanstalk — Amazon Linux 2023 / .NET 10"]
    DonorEnv["aws-asm3-donor-env"]
    AdminEnv["aws-asm3-admin-env"]
  end

  APIGW["API Gateway HTTP API<br/>aws-asm3-http-api"]
  LambdaApi["Lambda aws-asm3-api<br/>.NET 10 / DISABLE_OUTBOX_PROCESSOR=true"]
  LambdaOutbox["Lambda aws-asm3-outbox-worker<br/>placeholder stub"]
  RDS[("RDS PostgreSQL 16<br/>aws-asm3-postgres")]
  ReceiptsS3[("S3 receipts bucket")]
  AnalyticsS3[("S3 analytics bucket")]
  Glue["Glue catalog aws_asm3_donation"]
  Athena["Athena workgroup aws-asm3-analytics"]
  CW["CloudWatch Logs<br/>7-day retention"]

  DonorBrowser --> DonorEnv
  AdminBrowser --> AdminEnv
  DonorEnv -->|"DonationApiClient"| APIGW
  AdminEnv -->|"DonationApiClient"| APIGW
  APIGW --> LambdaApi
  LambdaApi --> RDS
  LambdaApi --> ReceiptsS3
  LambdaOutbox -.->|"not a real poller"| RDS
  RDS -.-> Glue
  Glue --> Athena
  Athena --> AnalyticsS3
  LambdaApi --> CW
  APIGW --> CW
  DonorEnv --> CW
  AdminEnv --> CW
  RDS --> CW
```

AWS Academy `voclabs` cannot create CloudFront distributions (`cloudfront:CreateDistribution` denied). UIs are reached on Elastic Beanstalk CNAMEs. The request Lambda does not poll the outbox; seed/migration drains it. Live donor gifts stay pending until a real worker exists.

## 2. Donate click — one command, then outbox

The public donate form posts **one** command: `POST /api/v1/donations` → `UserMakesDonationCommand`. That write creates or reuses a Contact (by email) and always creates a PaymentMethod.

- **One-time gift:** creates a pending `Transaction` with no payment schedule and raises `TransactionCreated`.
- **Recurring gift:** creates a `PaymentSchedule` and raises `PaymentScheduleCreated`. The outbox then opens the first transaction.

A hosted poller (`OutboxProcessor`, every 5s) publishes pending outbox rows, then MediatR handlers send the **next command**. Generating later recurring charges is out of scope: a recurring donate records one schedule and one first transaction.

The six REST writes (`POST /contacts`, `/payment-methods`, `/payment-schedules`, `/transactions`, `/journals`, `/receipts`) still exist for admin / stepwise APIs. The donor UI no longer uses them for a gift.

```mermaid
sequenceDiagram
  actor Donor
  participant UI as Donation.UI
  participant API as Donation.Api
  participant DB as PostgreSQL / RDS
  participant Outbox as OutboxProcessor
  participant Store as MinIO / S3

  Donor->>UI: Submit donate form<br/>(one-time or recurring)
  UI->>API: POST /api/v1/donations
  API->>DB: Contact + PaymentMethod + Transaction (pending) or PaymentSchedule<br/>+ OutboxMessages (same transaction)
  API-->>UI: 201 ContactId, PaymentMethodId, TransactionId or PaymentScheduleId
  UI-->>Donor: Thank you

  loop every 5s (local API only)
    Outbox->>API: ProcessOutboxMessagesCommand
    API->>DB: claim pending OutboxMessages
    Note over API,DB: PaymentScheduleCreated → ProcessDonationTransaction<br/>TransactionCreated → QueueTransactionPending<br/>TransactionPending → CompleteDonationTransaction<br/>TransactionCompleted → receipt + journal in parallel
    opt payment succeeded
      API->>Store: upload QuestPDF receipt
      API->>DB: Receipt + Journal
    end
  end
```

Admin **reads** the same tables (`GET /contacts`, `/transactions`, `/journals`, `/receipts`) and downloads PDFs with `Accept: application/pdf`. Trace outbox rows with `GET /api/v1/outbox-messages?correlationId=` or `?idempotencyKey=`.

## 3. Donation CQRS pipeline

There is no `Donation` entity. **PaymentSchedule** is recurring intent only. A one-off gift is a `Transaction` with no schedule. Journal stays in the same PostgreSQL database (not a separate ledger DB).

```mermaid
flowchart TD
  UI["Blazor Server<br/>Hope & Help"]
  API["ASP.NET Core 10 API"]

  C1["UserMakesDonationCommand"]
  H1["UserMakesDonationCommandHandler"]

  OneOff["Contact + PaymentMethod + pending Transaction"]
  Recurring["Contact + PaymentMethod + PaymentSchedule"]

  EContact["ContactCreated"]
  EMethod["PaymentMethodCreated"]
  ESchedule["PaymentScheduleCreated"]
  ETxnCreated["TransactionCreated"]

  DB[("PostgreSQL / RDS")]
  O["Outbox / Domain Event Dispatcher"]

  C2["ProcessDonationTransactionCommand"]
  H2["ProcessDonationTransactionCommandHandler"]

  CPending["QueueTransactionPendingCommand"]
  EPending["TransactionPending"]

  C3["CompleteDonationTransactionCommand"]
  H3["CompleteDonationTransactionCommandHandler"]

  EOk["TransactionCompleted"]
  EFail["TransactionFailed"]

  C4["GenerateDonationReceiptCommand"]
  H4["GenerateDonationReceiptCommandHandler"]
  EReceiptCreated["ReceiptCreated"]
  EReceiptGen["ReceiptGenerated"]

  C5["CreateJournalEntryCommand"]
  H5["CreateJournalEntryCommandHandler"]
  EJournal["JournalEntryCreated"]

  UI --> API --> C1 --> H1
  H1 -->|"one-off"| OneOff
  H1 -->|"recurring"| Recurring
  OneOff -->|"raises"| EContact
  OneOff -->|"raises"| EMethod
  OneOff -->|"raises"| ETxnCreated
  Recurring -->|"raises"| EContact
  Recurring -->|"raises"| EMethod
  Recurring -->|"raises"| ESchedule
  H1 --> DB
  EContact --> O
  EMethod --> O
  ESchedule --> O
  ETxnCreated --> O

  O --> C2 --> H2 --> ETxnCreated
  O --> CPending --> EPending
  EPending --> C3 --> H3
  H3 -->|"success"| EOk
  H3 -->|"failure"| EFail
  EOk --> DB
  EFail --> DB
  EOk --> C4
  EOk --> C5
  C4 --> H4 --> EReceiptCreated --> EReceiptGen
  EReceiptGen --> DB
  H4 -->|"QuestPDF"| Store["MinIO / S3"]
  C5 --> H5 --> EJournal --> DB
```

### Command flow

```text
UserMakesDonationCommand
        │
        ▼
UserMakesDonationCommandHandler
        │
        ├── one-off  → Contact + PaymentMethod + pending Transaction
        │                 ├── ContactCreated (new contacts only)
        │                 ├── PaymentMethodCreated
        │                 └── TransactionCreated
        │
        └── recurring → Contact + PaymentMethod + PaymentSchedule
                          ├── ContactCreated (new contacts only)
                          ├── PaymentMethodCreated
                          └── PaymentScheduleCreated
                                │
                                ▼
                    ProcessDonationTransactionCommand
                                │
                                ▼
                       TransactionCreated
                                │
                                ▼
                    QueueTransactionPendingCommand
                                │
                                ▼
                       TransactionPending
                                │
                                ▼
                    CompleteDonationTransactionCommand
                                │
                          ┌─────┴─────┐
                          ▼           ▼
              TransactionCompleted  TransactionFailed
                          │
                     ┌────┴─────┐
                     ▼          ▼
              GenerateDonation  CreateJournalEntry
              ReceiptCommand    Command
                     │          │
                     ▼          ▼
              ReceiptCreated    JournalEntryCreated
              ReceiptGenerated
```

Production API completion uses `SucceededDonationTransactionOutcome` (always succeed). Seed uses `RandomDonationTransactionOutcome` (about 50/50 fail). Failure stops: no receipt, no journal.

### Minimal commands

```text
UserMakesDonationCommand
ProcessDonationTransactionCommand
QueueTransactionPendingCommand
CompleteDonationTransactionCommand
GenerateDonationReceiptCommand
CreateJournalEntryCommand
```

### Minimal domain events

```text
ContactCreated
CountryCreated
PaymentMethodCreated
PaymentScheduleCreated
TransactionCreated
TransactionPending
TransactionCompleted
TransactionFailed
ReceiptCreated
ReceiptGenerated
JournalEntryCreated
```

### Aggregate / state (as implemented)

```text
PaymentMethod            ← DisplayName + PaymentType
PaymentSchedule          ← recurring intent only (interval is never one-off)
 ├── Contact
 ├── PaymentMethod
 └── Transaction         ← first charge for the schedule
Transaction              ← one-off gift has no schedule
       │
       ├── Pending
       ├── Succeeded     ← TransactionCompleted
       └── Failed        ← TransactionFailed
```

Direct `Transaction.Create` (the six-call REST path) still defaults status to **Succeeded** so admin/stepwise APIs are not stuck pending.

`TransactionCompleted` triggers **two independent application commands in parallel** (separate DI scopes, so they do not share a DbContext):

```text
TransactionCompleted
       │
       ├──────────────► GenerateDonationReceiptCommand  → QuestPDF → S3/MinIO
       │
       └──────────────► CreateJournalEntryCommand
```

## 4. Inside one API request (stepwise REST still)

Every create/update/delete on the existing six resources still follows Country-style CQRS: controller → MediatR command → handler → domain entity → EF repository → PostgreSQL. Query DTOs are kebab-case (`transaction-id`, `do-not-email`); write bodies are camelCase.

```mermaid
flowchart TB
  HTTP["HTTP POST /api/v1/transactions<br/>camelCase JSON"]
  Ctrl["TransactionController.Create"]
  Cmd["CreateTransactionCommand"]
  H["CreateTransactionCommandHandler"]
  V["Load Contact, Schedule, Method<br/>reject if missing / wrong owner"]
  E["Transaction.Create — domain rules"]
  R["ITransactionRepository.AddAsync"]
  EF["ApplicationDatabaseContext"]
  DB[("Transactions row")]

  HTTP --> Ctrl --> Cmd --> H --> V --> E --> R --> EF --> DB
```

Donate is different: `POST /api/v1/donations` only writes the first aggregates + outbox; later writes happen on later poller cycles.

## Report notes

| In these diagrams | Not in this path |
|---|---|
| Two Blazor sites → one API → Postgres | JWT / IdentityServer |
| One donate command + transactional outbox | Payment processor / webhooks |
| Receipt bytes in S3 (or MinIO locally), metadata in `Receipts` | SES / SNS notify (logged only) |
| Journal in the **same** Postgres as donations | Separate ledger database |
| Recurring = flag + interval on the schedule | Generating later recurring transactions |
| Redis only beside the write path | Browser talking to the API directly |
| Production always succeeds; seed is 50/50 | Real gateway result |
| API Gateway + Lambda + EB + RDS | CloudFront (blocked on AWS Academy) |
