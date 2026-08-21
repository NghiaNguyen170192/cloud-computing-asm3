# AWS rubric alignment — NetCore.Donation (COSC29800 A3)

Maps [ASSESSMENT_3_S2-1.pdf](ASSESSMENT_3_S2-1.pdf) + [rubric.md](rubric.md) to this project’s **current local state** and the **AWS services you should implement for marks**.

Due: 12 Sep 2026 · Weight: 40 marks · Demo: Week 12 · Zip submission + solution architecture document.

---

## 1. Rubric scoreboard (what you are marked on)

| Block | Max | How you earn it |
|---|---:|---|
| Project idea + tech selection | 2 | Donation NFP idea + justified AWS map |
| Skill / tools learning | 3 | Demo fluency (CLI, deploy, SDK, Aspire→AWS) |
| **Cloud services implementation** | **25** | Fully **automated** services (invoked by UI/code/other services — **not** console/CLI-only) |
| Solution architecture document | 10 | Summary 0.5 · Intro 1 · Related work 1 · Architecture diagrams 5 · Descriptions 1 · Data/APIs 1 · Refs 0.5 |
| **Total** | **40** | |

### Cloud services marking rules (Criteria 1–5)

1. Services must sit under: **Compute, Containers, Storage, Networking & Content Delivery, Database, Analytics**.
2. Must be **fully implemented and automated** by the app (donate click → service call), not “I created it in the console”.
3. **6 pts / type:** Elastic Beanstalk · Lambda · API Gateway · ECS · EMR.
4. **3 pts / type:** other services in those categories (RDS, S3, CloudFront, ElastiCache, Athena, …).
5. **Third-party APIs:** 2 pts each, **max 2 graded** (= 4).
6. **No double-count:** if Beanstalk/ECS/EMR/Lambda “contains” EC2 + S3 as part of that service, you get **only** the premium 6 — not EC2/S3 again for that nesting. Use S3 **independently** for receipt PDFs (SDK from the app) so S3 is its own justified service.

### Services that help the product but usually **do not** score under Criteria 1–5

| Service | Why |
|---|---|
| **IAM** | Spec example: no marks |
| **SES** | Spec example: no marks (still useful for donor email) |
| **SNS / SQS / EventBridge** | Application Integration — **outside** the listed Console categories |
| **CloudWatch** | Management/Governance — usually not scored as Compute/Storage/… |

Use them for a solid demo and architecture story; **do not rely on them for the 25 pts**.

---

## 2. Current project state vs assignment need

| Concern | Local today | Required for A3 demo | Rubric category |
|---|---|---|---|
| Donor + admin UI | Blazor Server `:6010` / `:6020` | Live URL on AWS | Compute (EB) or Containers (ECS) |
| HTTP API + CQRS | Kestrel API `:6000` | Deployed API | API Gateway + Lambda **or** EB |
| Domain data | Aspire PostgreSQL | Managed DB | **RDS** (Database, 3) |
| Receipt PDFs | MinIO + `AWSSDK.S3` | Real AWS bucket | **S3** (Storage, 3) |
| Async donate pipeline | Outbox + in-process poller (5s) | Cloud-triggered worker | **Lambda** (Compute, 6) — keep `OutboxMessages` table |
| Cache / idempotency | Redis (Aspire) | Optional managed cache | **ElastiCache** (Database, 3) |
| Notify email/SMS | Pref flags only | Optional SES/SNS | Product only (often 0 rubric pts) |
| CDN | None | Optional front door | **CloudFront** (Networking, 3) |
| Analytics | Admin grids only | Nice for rubric | **Athena** (Analytics, 3) |

Locked product decisions stay: MediatR CQRS, transactional outbox, one-off vs recurring donate, no separate ledger DB, Application layer has no AWS SDK types.

---

## 3. Recommended AWS stack (fit this app + maximise marks)

Target **≥ 25** from services alone (rubric caps the block at 25). Prefer **one** hosting style for UIs.

### Premium (18 pts)

| # | Service | Pts | Role in *this* donation app | Automate how |
|---|---|---:|---|---|
| 1 | **API Gateway** | 6 | Public `/api/v1/*` for donate + admin reads | Blazor `DonationApiClient` → Gateway HTTP APIs |
| 2 | **Lambda** | 6 | (A) API handlers **and/or** (B) outbox/async worker that runs `ProcessOutboxMessagesCommand` | Donate/outbox events invoke Lambda; no manual console runs |
| 3 | **Elastic Beanstalk** | 6 | Host **Blazor Server** donor + admin (ASP.NET needs sticky sessions / long-lived SignalR) | User opens site → EB environment; deploys via CI or EB CLI once, then traffic only |

*Alternative to Beanstalk:* **ECS** (also 6) for the same UIs — pick **one** of EB or ECS, not both for hosting the same thing.

### Standard (15 pts → enough with premium to fill 25)

| # | Service | Pts | Role | Automate how |
|---|---|---:|---|---|
| 4 | **RDS PostgreSQL** | 3 | Contacts, schedules, transactions, journals, receipts metadata, **OutboxMessages** | App connection string; migrations on deploy |
| 5 | **S3** | 3 | `receipts/{id}.pdf` (already abstracted as `IReceiptDocumentStorage`) | Succeeded gift → `PutObject`; admin download → `GetObject` |
| 6 | **CloudFront** | 3 | CDN in front of EB (or static assets / receipt download) | Browser hits CloudFront URL only |
| 7 | **ElastiCache (Redis)** | 3 | Replace Aspire Redis (idempotency / cache) | API uses Redis endpoint automatically |
| 8 | **Athena** | 3 | Analytics: query exported donation/transaction CSVs in S3 (admin “insights”) | Admin button or scheduled export → Athena query → chart |

**Mark math:** 6+6+6+3+3+3+3+3 = **33** → capped at **25**. Drop Athena or ElastiCache if time is tight; keep API Gateway + Lambda + EB + RDS + S3 first (**21**).

### Optional third-party (up to +4)

| API | Pts | Idea |
|---|---:|---|
| Google Maps / Address validation | 2 | Donor address on donate form |
| SendGrid **or** Twilio | 2 | Only if you want a graded non-AWS notify path (SES itself is ungraded) |

---

## 4. Architecture to implement (rubric-safe)

```text
Browser
  → CloudFront (3)
    → Elastic Beanstalk: Blazor UI donor + admin (6)
         → API Gateway (6)
              → Lambda: ASP.NET / Minimal API CQRS surface (6)
                   → RDS PostgreSQL (3)
                   → ElastiCache Redis (3)
                   → S3 receipts (3)
              → Lambda: Outbox processor (same Lambda type — still one "Lambda" for marks)
                   → MediatR pipeline (receipt + journal)
                   → S3 / RDS
  → Athena (3) reads analytics exports from S3
```

**Outbox stays in RDS.** Replace only the *poller host*: EventBridge Scheduler / CloudWatch Events → Lambda is fine for ops, but score Lambda + keep claiming RDS/S3, not EventBridge.

**Do not** claim EC2 or “Beanstalk’s internal S3” as extra 3-pt services. Claim **S3** only for the receipt bucket your code calls.

---

## 5. What *not* to build for marks

| Temptation | Why skip / de-prioritise |
|---|---|
| EMR | 6 pts but heavy for this domain; Athena is enough Analytics |
| Second premium host (EB **and** ECS) | Wasted effort; still one “type” story if same role — pick one |
| Manual console-only VPC toys | Not automated by client → 0 |
| SES/SNS as “my cloud marks” | Spec/example: SES no marks; SNS category risk |
| Rebuilding Assessment 2 | Forbidden |

---

## 6. Implementation order (aligned with Weeks 7–12)

1. **S3 receipts** — drop MinIO `ServiceUrl`; IAM role + bucket (code already S3-shaped).
2. **RDS** — restore DB, connection string, run migrations + seed.
3. **Lambda + API Gateway** — package API (or thin API + outbox worker Lambda).
4. **Elastic Beanstalk** — deploy Blazor UIs; point to Gateway URL.
5. **CloudFront** — front EB (or static).
6. **ElastiCache** — if Redis still used in prod path.
7. **Athena + small admin chart** — analytics story for diagrams + demo.
8. **Solution architecture document** — live URL, repo link, diagrams from [DATA_FLOW.md](DATA_FLOW.md) redrawn with AWS names.
9. **Demo script** — donate one-off + recurring → outbox Lambda → journal + S3 PDF → admin list → Athena insight.

---

## 7. Demo checklist (examiner “automated” test)

- [ ] Open **live** donor URL (CloudFront/EB) — no localhost.
- [ ] Submit donate → row in **RDS** without console SQL.
- [ ] Wait for worker → **S3** object appears; receipt download works from admin UI.
- [ ] Show **API Gateway** request in browser Network tab or X-Ray/logs only as support (marks = Gateway itself).
- [ ] Show **Athena** query result or admin chart fed by Athena.
- [ ] Walk architecture diagram: every box is a scored service + purpose.

---

## 8. Document section ↔ rubric

| Document part | Pts | Source in this repo |
|---|---:|---|
| Summary | 0.5 | Short objective from [SCOPE.md](SCOPE.md) |
| Introduction | 1 | Motivation / high-level / beneficiaries (NFP donors + staff) |
| Related work | 1 | Microsoft NFP / Dynamics donation apps; other cloud donation platforms |
| System architecture | 5 | Redraw [DATA_FLOW.md](DATA_FLOW.md) with AWS services above |
| System descriptions | 1 | One paragraph per service in §3 |
| Datasets / APIs / structures | 1 | Aggregates in SCOPE + `POST /api/v1/donations` contract |
| References | 0.5 | AWS docs, IEEE refs |

---

## 9. Locked service choices for this assignment

| Decision | Choice |
|---|---|
| UI host | **Elastic Beanstalk** (not ECS unless EB is blocked in Academy) |
| API edge | **API Gateway + Lambda** |
| Async | **Lambda** reading **RDS OutboxMessages** (keep table) |
| Data | **RDS PostgreSQL** |
| Files | **S3** receipt bucket |
| CDN | **CloudFront** |
| Cache | **ElastiCache Redis** (if time) |
| Analytics | **Athena** on S3 exports (if time) |
| Notify | SES/SNS optional for demo polish — **not** mark drivers |

When this plan changes, update [SCOPE.md](SCOPE.md) and [CONTINUATION.md](CONTINUATION.md) together.
