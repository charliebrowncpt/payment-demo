# AGENTS.md

Guidance for AI coding agents working in this repository. Read it fully before changing code.
Where this file and the code disagree, the code is current. Tell the user and propose an update to this file.

---

## 1. Project

Zapper customers pay a merchant by scanning a QR code in the app and paying with a card they stored earlier.
The backend submits the payment to **Acme Pay** (a fictional payment service provider) and tracks it through its lifecycle.
Zapper is a **payment facilitator**: merchants are registered as sub-merchants under Zapper's Acme account.
The acquiring bank is **FNB**.

This is an assessment project. The Acme stub is supporting infrastructure and is not assessed. The service, the integration, persistence, notification handling and the daily summary are.

Out of scope: caller authentication, pagination, batch operations, infrastructure as code, load testing.

---

## 2. Stack and layout

- .NET 8, FastEndpoints, EF Core, FluentValidation
- AWS SQS and Lambda (managed .NET runtime). LocalStack for local work.
- Resilience via `Microsoft.Extensions.Http.Resilience` (Polly v8) in the relay Lambda

```
payment-demo/
  PaymentsDemo.Api/
    Features/Payments/Endpoints/SubmitPayment/      SubmitPaymentEndpoint.cs, SubmitPaymentRequest.cs
    Infrastructure/
      Acme/                                         AcmeEnvelopeBuilder.cs, AcmeOptions
      Data/
        ApplicationDbContext.cs
        Converters/                                 ValueObjectConverters.cs
        Domain/                                     Customer, BankCard, Merchant, SubMerchant, Payment, PaymentAdjustment
        Domain/Models/ValueObjects/                 *ValueObject.cs
      Messaging/                                    IPaymentIntentQueue, SqsPaymentIntentQueue
  PaymentsDemo.AcmeServiceMessageRelay/             Lambda: relays payment-intents to Acme
  notification-lambda/                              Lambda: consumes Acme notifications
  acme-stub/                                        Acme stub (not assessed)
  scripts/                                          LocalStack setup and deploy scripts
```

Folders are approximate. **A namespace must match its folder.** Value objects live in `PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects`.

---

## 3. Commands

Adjust paths to the real project names.

```bash
dotnet build
dotnet test

# EF Core migrations
dotnet ef migrations add <Name> --project PaymentsDemo.Api
dotnet ef database update --project PaymentsDemo.Api

# Lambda package (the relay shown here)
dotnet publish PaymentsDemo.AcmeServiceMessageRelay -c Release -o publish
(cd publish && zip -qr ../relay.zip .)

# Local infrastructure
docker compose up -d
bash scripts/localstack-setup.sh
./scripts/deploy-notification-handler.sh [create|update|auto]
```

The LocalStack scripts and Lambda deploy commands have **not** been run end to end. Check their output on first use.

---

## 4. Architecture

```
Mobile app
  | POST /api/v1/payments  -> 202 + Location
  v
API  --(stores Pending payment)--> database   (the API is the ONLY writer)
  | publishes Acme envelope
  v
SQS payment-intents --> acme-relay Lambda --> Acme POST /api/payment
                             | publishes result
                             v
                      SQS payment-responses --> API updates the payment

Acme webhooks (Payment Advice, Switch Transaction Status)
  --> SQS notifications --> notification-handler Lambda --> API updates the payment
```

- The app never sees Acme's answer in the submit response. It polls `GET /api/v1/payments/{id}` (the `Location` header).
- The Lambdas do not touch the database. How results and notifications are delivered to the API is still open (see section 13).

### Queues

| Queue | Carries | Consumer | Settings |
|---|---|---|---|
| `payment-intents` | Full Acme envelope built by the API | `acme-relay` | Visibility timeout 360s, DLQ after 5 receives |
| `payment-intents-dlq` | Failed intents | Manual inspection | 14 days retention |
| `payment-responses` | Relay results: Approved, Declined, Rejected | API | |
| `notifications` | Inbound Acme events | `notification-handler` | Batch size 10, timeout 30s |

---

## 5. Domain model (DDD)

One bounded context, **Payments**, with three aggregates.

| Aggregate | Root | Children | Value objects |
|---|---|---|---|
| Customer | `Customer` | `BankCard` | `ContactDetailValueObject` |
| Merchant | `Merchant` | `SubMerchant` | `SubMerchantCodeValueObject`, `TradingNameValueObject`, `AddressValueObject`, `MerchantCategoryCodeValueObject`, `CardAcceptorNameValueObject` |
| Payment | `Payment` | `PaymentAdjustment` | uses `SubMerchantCodeValueObject` by reference |

### Rules

- **Reference other aggregates by ID only.** `Payment` holds `CustomerId`, `CardId` and `SubMerchantCode`. No navigation properties across aggregates.
- **Only aggregate roots get a `DbSet`.** Expose `Customers`, `Merchants`, `Payments`. Never add a `DbSet` for `BankCard`, `SubMerchant`, `PaymentAdjustment` or any value object.
- **State changes go through methods.** Setters are private. Child constructors are `internal`, so only the root can create them.
- **Value objects are immutable, validated on creation, and equal by value.** Invalid input throws `DomainException`.
- **Deactivate, never delete** cards, sub-merchants and merchants. Payments reference them.
- **Internal IDs are Guids.** The external business key for a payee is `SubMerchantCodeValueObject`, which is what Acme and the QR code carry.
- **Ubiquitous language.** *Payment* is what the app tracks. *Transaction* is Acme's word and appears only in the Acme mapping (`TransactionID`). Don't name our own entity `Transaction`.
- **One aggregate per transaction.** Rules spanning aggregates (for example, no card removal while a payment is in flight) run in the application layer.

### Value objects

| Type | Rule | Stored as | Acme field |
|---|---|---|---|
| `SubMerchantCodeValueObject` | 1-15 ASCII letters or digits, uppercased | One column (converter) | `SubMerchantID` |
| `MerchantCategoryCodeValueObject` | Exactly 4 digits | One fixed-length column (converter) | `MerchantCategoryCode` |
| `TradingNameValueObject` | 1-19 characters, no `*` | One column (converter) | `CardAcceptorName` positions 5-23 |
| `AddressValueObject` | City up to 50 stored in full, region 2 letters, country 2 letters | Owned type, columns on `SubMerchants` | `CardAcceptorName` positions 24-40 |
| `ContactDetailValueObject` | Email required, phone optional | Owned type, columns on `Customers` | `Email` |
| `CardAcceptorNameValueObject` | 40 characters, computed | **Not stored** | `CardAcceptorName` |

Trading name is capped at 19 because FNB reads positions 1-23 as `ZAP*<name>`. The cap is derived from `CardAcceptorNameValueObject`, so keep one source for the prefix.

---

## 6. EF Core rules

All tables are in the `payments` schema.

- **Apply configurations.** `OnModelCreating` must call `modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly)`. If it doesn't, EF treats value objects as entities and fails at startup.
- **Single-value objects use value converters**, registered once in `ConfigureConventions` via `configurationBuilder.ConfigureValueObjects()` (see `ValueObjectConverters.cs`). Don't repeat `HasConversion` in entity configurations.
- **Multi-field objects use `OwnsOne`** in the owner's configuration, with explicit column names (`AddressCity`, `AddressRegionCode`, `AddressCountryCode`). They need a private parameterless constructor.
- **Computed objects are never mapped.** Don't add a property of type `CardAcceptorNameValueObject` to an entity. Use a method.
- **Collections of children** (`Cards`, `SubMerchants`, `Adjustments`) use a private `List<T>` backing field with `UsePropertyAccessMode(PropertyAccessMode.Field)` and a read-only public view.
- **`ValueGeneratedNever` on all `Id`s.** The domain generates Guids.
- **Enums stored as strings** (`Status`, `MerchantStatus`, `AdjustmentType`).
- **`HasAlternateKey(s => s.Code)` on `SubMerchant`.** `Payment.SubMerchantCode` has a foreign key to it with `Restrict`.
- **Delete behaviour:** `Restrict` for customer to cards and merchant to sub-merchants, `Cascade` for payment to adjustments.

### Unique indexes (the concurrency guard)

| Index | Purpose |
|---|---|
| `Payments.IdempotencyKey` | One payment per key |
| `Payments.TransactionId` | Same value as the key. Mirrors Acme's replay rule |
| `Payments.RetrievalReferenceNumber` | Acme requires this to be unique across the account (E09) |
| `PaymentAdjustments (PaymentId, EventId)` | Dedupes Switch events by `transactionUUID` |
| `BankCards.Token` | One card per token |

### Common startup errors

| Error | Cause | Fix |
|---|---|---|
| "No suitable constructor was found for ...ValueObject" | A single-value object has no converter | Add the converter and the `ConfigureValueObjects()` call |
| "The entity type 'AddressValueObject' requires a primary key" | Owned type not declared with `OwnsOne`, or configurations not applied | Check `ApplyConfigurationsFromAssembly` and the `OwnsOne` block |

---

## 7. Submit payment flow

`POST /api/v1/payments`

Headers: `X-Idempotency-Key` (UUIDv4, required), `X-Api-CorrelationId` (optional GUID).
Body: `customerId` (Guid), `cardId` (Guid), `subMerchantId` (string), `amount` (long, lowest unit, so 1999 is R19.99).

Checks run in this order. Nothing is written or queued until they all pass.

1. **Validate.** Key is UUIDv4. Correlation ID is absent (generate one) or a GUID (otherwise 400). Amount is positive. Sub-merchant code parses.
2. **Idempotency.** A stored key replays that payment without re-running checks. A different body returns 422.
3. **Customer and card.** One query. The customer must be active, and the card must be active and owned by them. Any miss returns the same 404.
4. **Expiry.** Checked against SAST. Expired returns 422.
5. **Merchant and sub-merchant.** `Merchant.CanAcceptPaymentsFor(code)` decides. Unknown is 404, unable to accept is 422.
6. **Build** `Payment.Submit(...)`, then attach the envelope from `AcmeEnvelopeBuilder`.
7. **Save.** On `DbUpdateException`, re-query: key found means replay, retrieval reference taken means 409, otherwise rethrow.
8. **Publish** to `payment-intents`, then `MarkEnqueued`. Publish only if the payment is `Pending` and has never been queued.
9. **Return 202** with `Location: /api/v1/payments/{id}` and echo `X-Api-CorrelationId`.

| Status | Code | When |
|---|---|---|
| 202 | | Accepted, or replay of a stored payment |
| 400 | | Validation failed |
| 404 | `CARD_NOT_FOUND`, `SUB_MERCHANT_NOT_FOUND` | |
| 409 | `RRN_IN_USE` | Another key holds this retrieval reference. Use a new key. |
| 422 | `CARD_EXPIRED`, `SUB_MERCHANT_INACTIVE`, `IDEMPOTENCY_KEY_REUSED`, `INVALID_PAYMENT` | |
| 503 | `QUEUE_UNAVAILABLE` | Retry with the same key. `Retry-After: 5` is sent. |

Responses use camelCase. An idempotency key must be used for one payment only. A new attempt after a decline needs a new key, or Acme replays the declined result.

---

## 8. Identifier mapping

| Our value | Acme field | Source |
|---|---|---|
| `AcmeOptions.MerchantId` | `MerchantID` | Configuration. Never from the client. |
| Idempotency key, with hyphens | `TransactionID` | `X-Idempotency-Key` |
| Idempotency key, without hyphens | `OrderNumber` | Derived |
| First 12 characters of `OrderNumber` | Retrieval Reference Number | Derived. Used to match Switch webhooks. |
| `SubMerchant.Code` | `SubMerchantID` | QR code, resolved by the API |
| `SubMerchant.MerchantCategoryCode` | `MerchantCategoryCode` | Sub-merchant record |
| `SubMerchant.BuildCardAcceptorName()` | `CardAcceptorName` | Built from the record |
| `BankCard.Token` | `Card.Token` | Our store. Acme never returns it. |
| `Customer.ContactInformation.Email` | `Email` | Customer record |

`TransactionID` and `OrderNumber` come from the same key, so the key must be UUIDv4. A time-based UUID shares its first characters with its neighbours, which causes E09 collisions.

---

## 9. Payment state machine

`Pending`, `Approved`, `Declined`, `Rejected`, `Unknown`, `Reversed`, `Refunded`

| From | To | Trigger | Rule |
|---|---|---|---|
| Pending | Approved, Declined, Rejected | Acme response or Payment Advice | Idempotent on repeat |
| Pending | Unknown | Timeout | Ignored if a result already arrived |
| Unknown | Approved, Declined, Rejected | Retry result or notification | Same as Pending |
| Approved | Reversed | Switch `Reversal` | Full settled amount only, and only with no refunds |
| Approved | Approved | Switch `Refund` (partial) | Amount within `RefundableAmount` |
| Approved | Refunded | Switch `Refund` | Balance reaches zero |

- `SettledAmount` is `AuthAmount ?? Amount`. A partial authorisation can approve less than requested.
- Duplicate adjustments (same `EventId`) are ignored. Out-of-order events throw `DomainException`. The consumer decides whether to retry later or park the event. It must not silently discard it.
- A timeout is **not** a decline. The payment stays `Pending` or becomes `Unknown` until a retry or notification resolves it.

---

## 10. Acme integration facts

- Field names are PascalCase. Ignore unknown fields in responses, because Acme may add them.
- Amounts are integers in the lowest unit. Currency defaults to ZAR.
- `MessageDateTime` is SAST (UTC+02:00), ISO 8601, no offset, milliseconds. Format with `CultureInfo.InvariantCulture`.
- Approval is response code `00`. `Success` status with a non-`00` code is an acquirer decline.
- Errors: 400/E01 malformed, 401/E02 bad key, 409/E09 order prefix in use, 429/E29 over 20 requests per second (honour `Retry-After`), 5xx/E50 degraded.
- Acme stores the outcome per `TransactionID` for 24 hours and replays it, so retrying with the same `TransactionID` is safe.
- `CardAcceptorName` is 40 characters by position: name 1-23 as `ZAP*<name>`, city 24-36, region 37-38, country 39-40.

### Webhooks

| | Payment Advice | Switch Transaction Status |
|---|---|---|
| Carries | Authorize, Capture, Payment results | `Reversal` and `Refund`, possibly days later |
| Auth | HTTP Basic, reject without credentials | `x-signature`: Base64 HMAC-SHA256 of the **raw body**, keyed with `ACME_WEBHOOK_SECRET` |
| Links to payment by | `TransactionID` | `rrn` |
| Event ID | None. Dedupe on `TransactionID` + `RequestType` + result | `transactionUUID` |
| Must respond | 200, or Acme retries three times | 200 |

- Verify the signature against the raw bytes **before** parsing. Use a constant-time comparison.
- Endpoints are public, so treat every delivery as untrusted until verified.

---

## 11. Lambdas

### acme-relay

- Reads `payment-intents`, forwards the body to Acme `/api/payment` **unchanged**, and publishes a result to `payment-responses`.
- Retry: in-process exponential backoff with jitter (3 retries, 8s per attempt, 30s total), then SQS redelivery.
- Outcomes: 200 is published as Approved or Declined. 400 and 409 are published as `Rejected`. 401 **throws** (a deployment problem that should alarm). 429, 5xx and timeouts throw so SQS redelivers.
- The handler returns `SQSBatchResponse` with `batchItemFailures`, so one bad message doesn't retry the batch. Start with batch size 1.
- Account-wide rate limit is 20 requests per second. Control it with the event source mapping's `MaximumConcurrency`, because an in-process limiter can't coordinate across instances.
- Lambda timeout 60s. SQS visibility timeout must stay at least 6 times that.

Configuration comes from the environment: `ACME_BASE_URL`, `ACME_API_KEY`, `RESPONSE_QUEUE_URL`. Production keys come from Secrets Manager, not plain environment variables.

### notification-handler

- Consumes `notifications`. Handler string: `<AssemblyName>::<Namespace>.<Class>::<Method>`, which must match the project.
- Uses the same verification and dedupe rules as section 10.

---

## 12. Conventions and hard rules

### Security

- **Never store a card number or CVC.** Only the Acme token and the expiry.
- **Never log** `Payment.AcmeEnvelopeJson`, SQS message bodies, card tokens or emails. The envelope holds the token and email.
- `MerchantID` and the Acme API key never reach the mobile app.
- Unknown and not-owned cards return the same 404, so IDs can't be enumerated.

### Code

- Configuration comes from the environment. Fail at startup if it's missing (`ValidateOnStart`).
- Money is `long` in minor units. Never use `float` or `decimal` for amounts.
- Store timestamps as UTC `DateTimeOffset`. Use SAST only for Acme formatting and the expiry check.
- Pass `CancellationToken` through. The one exception is the bookkeeping save after a successful publish, which uses `CancellationToken.None`.
- Don't catch broad `DbUpdateException` and assume a race. Re-query to confirm.
- Log with structured templates and include `CorrelationId` and `IdempotencyKey` in scope.
- Validation lives in the validator and in the domain. Handlers don't repeat it.
- Domain types never reference the Acme wire format. `AcmeEnvelopeBuilder` lives in infrastructure and the aggregate receives a finished string.

### Tests (cover these first)

- `CardAcceptorNameValueObject` is exactly 40 characters, including a city over 13 characters and a 19-character name
- `TradingNameValueObject` rejects 20 characters and `*`
- `SubMerchantCodeValueObject` rejects 16 characters and non-alphanumerics
- `Merchant.CanAcceptPaymentsFor` is false when the merchant is suspended or closed, or the sub-merchant is inactive
- `Customer.AddCard` rejects a duplicate token and an inactive customer
- `Payment`: duplicate adjustment ignored, refund over the balance rejected, full refund sets `Refunded`, reversal after refund rejected, `MarkUnknown` ignored after a result
- Endpoint: same key twice, same key with a different body, expired card, concurrent first requests, queue failure
- Relay: 409, 429 with `Retry-After`, 503 then success, timeout, 401 (use a fake Acme client)
- Webhooks: signature using the worked example in the Acme spec, duplicate delivery, out-of-order events

---

## 13. Open questions and status

Do not resolve these silently. Ask the user.

1. **Merchant versus sub-merchant.** The model assumes a merchant has many sub-merchants. Confirm this matches the assignment.
2. **How results reach the API.** The API is the only database writer, but how `payment-responses` and `notifications` events are delivered to it (an API-side consumer or an internal endpoint) is undecided.
3. **`MessageID` on retries.** The relay forwards the body unchanged, so retries reuse `MessageID`. Confirm Acme accepts that. If it doesn't, the relay must regenerate it.
4. **Timeout resolution.** Does resubmitting the same `TransactionID` resolve a timed-out payment, or must the notification do it?
5. **Transactional outbox.** The SQS publish sits in the request path. An outbox row written with the payment would remove that dependency. Recommended, not built.
6. **Card token uniqueness across customers.** The unique index on `BankCard.Token` assumes Acme never issues the same token twice.
7. **Email on the stored card.** The assignment lists it there. The model keeps it only on `Customer`.
8. **Daily summary.** Not defined in the spec or the assignment excerpt.
9. **Sequential Guids** if write volume grows (random Guid keys fragment clustered indexes).
10. **Seeding.** Seed through `Merchant.RegisterSubMerchant` and `Customer.AddCard` so invariants run on test data.

### Status

- The code in this project was drafted with AI assistance. **Treat it as not compiled** until `dotnet build` and the tests pass.
- Namespaces and some class names (`SubMerchantCodeValueObject`, `ContactDetailValueObject`) follow a pattern and need checking against the real files.
- The LocalStack scripts have not been run end to end.