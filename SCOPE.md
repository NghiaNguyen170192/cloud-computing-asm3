# Scope — Cloud-based donation management (COSC29800 Assignment 3)

Simulated donation workflow for a non-profit. **No real payments.** Donors submit a request; the system records a transaction, writes a journal line, stores a digital receipt, and later notifies by email or SMS according to `DoNotEmail` / `DoNotSms`.

Inspired by Microsoft NFP (`msnfp_*` / `kpmg_nfp_transactionjournal`) but **only** the donation spine. The Dynamics dump (`tables_mapping.txt`, gitignored) is not the schema we ship.

## In scope

| Aggregate | Role | Cardinality / FKs |
|---|---|---|
| Contact | Donor profile (including gender) + communication prefs | Country; many payment methods and schedules |
| PaymentMethod | Saved way to give (`DisplayName` + `PaymentType`) | **Contact 1 → many methods.** Inspired by `msnfp_paymentmethod`: an instrument used by a schedule **or** a one-off transaction. No reverse FKs to a single schedule/transaction. |
| PaymentSchedule | **Recurring gifts only** (`PaymentType` + interval ≠ one-off) | Contact; **this schedule’s current method** |
| Transaction | Posted gift (simulated), with `PaymentType` | Contact; **optional** schedule (null for one-off); **this gift’s method** |
| Receipt | Digital PDF in S3/MinIO | Contact; transaction; optional payment schedule (copied from the transaction when linked) |
| Journal | Ledger line for a posted gift | **Required transaction** (`kpmg_nfp_transactionjournal.kpmg_transaction`) |

Flow: one-off request → transaction → journal → receipt; recurring request → schedule → first transaction → journal → receipt → notify (SES/SNS later).

```
Contact 1──* PaymentMethod
Contact 1──* PaymentSchedule ──> PaymentMethod
Transaction ──> PaymentSchedule?, PaymentMethod, Contact
Receipt ──> Transaction?, PaymentSchedule?, Contact
Journal ──> Transaction
```

## Out of scope (Dynamics dump)

Appeals, events, packages, membership, gift batches, tributes, designations, donor commitments, grants/awards, bank runs, payment processors / merchant suite, receipt stacks, refunds, planned giving, and reverse FKs on PaymentMethod pointing at a single schedule or transaction.

## AWS map (rubric-aligned)

Full mark plan and anti-double-count rules: [`AWS_RUBRIC_ALIGNMENT.md`](AWS_RUBRIC_ALIGNMENT.md).

| Concern | AWS (for marks) | Local today |
|---|---|---|
| Donor + admin UI | **Elastic Beanstalk** (6) + optional **CloudFront** (3) | Blazor `:6010` / `:6020` |
| HTTP API | **API Gateway** (6) + **Lambda** (6) | Kestrel `:6000` |
| Async outbox worker | **Lambda** (same type) on `OutboxMessages` | In-process poller 5s |
| Data | **RDS PostgreSQL** (3) | Aspire Postgres |
| Receipt PDFs | **S3** (3) — app SDK, not “Beanstalk’s disk” | MinIO + `AWSSDK.S3` |
| Cache | **ElastiCache Redis** (3) if time | Aspire Redis |
| Analytics | **Athena** (3) if time | Admin grids only |
| Email / SMS | SES / SNS — **demo polish, usually 0 rubric pts** | Pref flags only |

Do **not** claim EC2 or nested Beanstalk storage as extra services. Prefer **EB or ECS**, not both, for UI hosting.
