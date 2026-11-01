using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.SQS;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Enums;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Enums;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Infrastructure.Acme;
using PaymentsDemo.Api.Infrastructure.Data;
using PaymentsDemo.Api.Infrastructure.Data.Domain;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Features.Payments.Endpoints.SubmitPayment;

public sealed class SubmitPaymentEndpoint(
    ApplicationDbContext db,
    IAmazonSQS queueClient,
    AcmeServiceEnvelopeBuilder envelopes,
    ILogger<SubmitPaymentEndpoint> logger) : Endpoint<SubmitPaymentRequest>
{
    private static readonly TimeSpan SastOffset = TimeSpan.FromHours(2);

    // camelCase responses, matching what the mobile app expects from a JSON API.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public override void Configure()
    {
        Post(RouteConstants.SubmitPayment);
        AllowAnonymous(); // add your own auth for callers of this endpoint
        Summary(s =>
        {
            s.Summary = "Submit a payment";
            s.Description = "Validates the request, stores the payment as Pending and queues it for submission to AcmePay.";
            s.Response<SubmitPaymentResponse>(202, "Accepted. Poll the Location header for the outcome.");
            s.Response<ErrorResponse>(400, "Validation failed, including a malformed X-Api-CorrelationId");
            s.Response<ErrorResponse>(404, "Card not found for this customer, or sub-merchant not found");
            s.Response<ErrorResponse>(409, "Payment reference already in use. Use a new idempotency key.");
            s.Response<ErrorResponse>(422, "Card expired, merchant unable to accept payments, or idempotency key reused with a different request");
            s.Response<ErrorResponse>(503, "Queue unavailable. Retry with the same idempotency key.");
        });
    }

    public override async Task HandleAsync(SubmitPaymentRequest req, CancellationToken ct)
    {
        // The validator has already rejected a malformed header. Null means "not supplied".
        Guid correlationId = req.CorrelationId ?? Guid.NewGuid();

        HttpContext.Response.Headers["X-Api-CorrelationId"] = correlationId.ToString();

        using var scope = logger.BeginScope(
            "CorrelationId:{CorrelationId} IdempotencyKey:{IdempotencyKey}",
            correlationId, req.IdempotencyKey
        );

        // Validated upstream. Normalising first means the hash is the same for "zap0004182" and "ZAP0004182".
        var code = SubMerchantCodeValueObject.Create(req.SubMerchantId);
        var requestHash = ComputeRequestHash(req, code);

        // Guard 1: idempotency. A stored key replays its payment, and no other check is re-run,
        // so a payment already made can't change because a card expired afterwards.
        // Tracked on purpose: the replay path may mark the payment as enqueued.
        var existing = await db.Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == req.IdempotencyKey, ct);

        if (existing is not null)
        {
            await ReplayAsync(existing, requestHash, correlationId, ct);
            return;
        }

        // Guards 2 and 3: active customer, active card owned by that customer, not expired.
        // An inactive customer, an unknown card and someone else's card all return the same 404,
        // so IDs can't be enumerated.
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.ToOffset(SastOffset).DateTime);

        var lookup = await db.Customers.AsNoTracking()
            .Where(c => c.Id == req.CustomerId && c.IsActive)
            .Select(c => new CustomerCardLookup(
                c.ContactInformation.EmailAddress,
                c.Cards
                    .Where(k => k.Id == req.CardId && k.CardStatus == BankCardStatus.Active)
                    .Select(k => new CardLookup(k.CardToken!, k.ExpiresOnMonth, k.ExpiresOnYear))
                    .FirstOrDefault()))
            .FirstOrDefaultAsync(ct);

        if (lookup?.Card is null)
        {
            await ErrorAsync(404, "CARD_NOT_FOUND", "Card not found.", correlationId, ct);
            return;
        }

        if (IsExpired(lookup.Card, today))
        {
            await ErrorAsync(422, "CARD_EXPIRED", "That card has expired.", correlationId, ct);
            return;
        }

        // Guard 4: merchant and sub-merchant. The aggregate decides, in one place.
        var merchant = await db.Merchants
                               .AsNoTracking()
                               .Include(m => m.SubMerchants)
                               .FirstOrDefaultAsync(m => m.SubMerchants.Any(s => s.Code == code),
                               ct);

        if (merchant is null)
        {
            await ErrorAsync(404, "SUB_MERCHANT_NOT_FOUND", "Sub-merchant not found.", correlationId, ct);
            return;
        }

        if (!merchant.CanAcceptPaymentsFor(code))
        {
            await ErrorAsync(422, "SUB_MERCHANT_INACTIVE", "This merchant can't accept payments right now.", correlationId, ct);
            return;
        }

        //If unique index is turned off, this will throw - there should only ever be a single sub-merchnat with a given code.
        var sub = merchant.SubMerchants.Single(s => s.Code == code);

        // The factory derives TransactionID, OrderNumber and the retrieval reference.
        Payment payment;
        try
        {
            payment = new Payment(
                customerId: req.CustomerId,
                cardId: req.CardId,
                subMerchantCode: code,
                amount: req.Amount,
                idempotencyKey: req.IdempotencyKey, 
                requestHash: requestHash,
                correlationId: correlationId,
                now: now
            );
        }
        catch (DomainException ex)
        {
            await ErrorAsync(422, "INVALID_PAYMENT", ex.Message, correlationId, ct);
            return;
        }

        // The envelope holds the card token and email. It is never logged.
        payment.AttachAcmeEnvelope(envelopes.Build(
            payment.OrderNumber,
            payment.TransactionId,
            payment.Amount,
            lookup.Card.Token,
            lookup.Email,
            sub,
            now
        ));

        db.Payments.Add(payment);

        var retrievalReferenceNumber = payment.RetrievalReferenceNumber;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique indexes on the key and the retrieval reference are the concurrency guard.
            // Work out which one fired by looking, so no provider-specific error codes are needed.
            db.ChangeTracker.Clear();

            var raced = await db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == req.IdempotencyKey, ct);

            if (raced is not null)
            {
                // A concurrent request with the same key won. Replay its result.
                await ReplayAsync(raced, requestHash, correlationId, ct);
                return;
            }

            var isRetrievalReferenceNumberTaken = await db.Payments
                                   .AsNoTracking()
                                   .AnyAsync(p => p.RetrievalReferenceNumber == retrievalReferenceNumber,
                                   ct);

            if (isRetrievalReferenceNumberTaken)
            {
                // Another key already holds this prefix. Acme would answer E09 for the same reason.
                logger.LogWarning("Retrieval reference {Rrn} already in use", retrievalReferenceNumber);
                await ErrorAsync(
                    409, 
                    "RetrievalReferenceNumber_IN_USE",
                    "This payment reference is already in use. Generate a new idempotency key and retry.",
                    correlationId,
                ct);

                return;
            }

            // Not a uniqueness clash, so it's a genuine database failure. Let it become a 500.
            throw;
        }

        await EnqueueAndRespondAsync(payment, correlationId, ct);
    }

    private async Task ReplayAsync(Payment existing, string requestHash, Guid correlationId, CancellationToken ct)
    {
        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            logger.LogWarning("Idempotency key reused with a different request");
            await ErrorAsync(
                422,
                "IDEMPOTENCY_KEY_REUSED",
                "This idempotency key was used with a different request.",
                correlationId,
                ct
            );

            return;
        }

        await EnqueueAndRespondAsync(existing, correlationId, ct);
    }

    private async Task EnqueueAndRespondAsync(Payment payment, Guid correlationId, CancellationToken ct)
    {
        // Publish only a Pending payment that has never been queued. That covers a first request and a
        // retry after the queue was down. Anything else is just reported, never published again.
        var needsPublish = payment.Status == PaymentStatus.Pending && payment.EnqueuedAtUtc is null;

        if (needsPublish && !await TryEnqueueAsync(payment, correlationId, ct))
        {
            HttpContext.Response.Headers["Retry-After"] = "5";
            await ErrorAsync(
                503,
                "QUEUE_UNAVAILABLE",
                "Payment could not be queued. Retry with the same idempotency key.",
                correlationId,
                ct
            );

            return;
        }

        HttpContext.Response.Headers["Location"] = $"/api/v1/payments/{payment.Id}";

        await SendJsonAsync(new SubmitPaymentResponse(
            payment.Id,
            payment.Status.ToString(),
            payment.TransactionId,correlationId
            ),
            202,
            ct
        );
    }

    private async Task<bool> TryEnqueueAsync(Payment payment, Guid correlationId, CancellationToken ct)
    {
        try
        {
            await queue.PublishAsync(payment.AcmeEnvelopeJson, correlationId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to queue payment {PaymentId}", payment.Id);
            return false;
        }

        // The message is on the queue, so this bookkeeping must not be cancelled by the client
        // disconnecting. If it still fails, the next retry publishes again, and Acme's
        // TransactionID replay returns the same outcome.
        payment.MarkEnqueued(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("Queued payment {PaymentId}", payment.Id);
        return true;
    }

    private Task ErrorAsync(int status, string code, string message, Guid correlationId, CancellationToken ct) =>
        SendJsonAsync(new ErrorResponse(code, message, correlationId), status, ct);

    private Task SendJsonAsync(object body, int status, CancellationToken ct) =>
        Send.StringAsync(JsonSerializer.Serialize(body, Json), status, "application/json", ct);

    private static bool IsExpired(CardLookup card, DateOnly today) =>
        card.ExpiryYear < today.Year ||
        (card.ExpiryYear == today.Year && card.ExpiryMonth < today.Month);

    // Covers the body fields only. A different correlation ID on a retry is not a different request.
    private static string ComputeRequestHash(SubmitPaymentRequest r, SubMerchantCodeValueObject code)
    {
        var canonical = $"{r.CustomerId:D}|{r.CardId:D}|{code.Value}|{r.Amount}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}

// Projection types for the customer and card lookup.
public sealed record CustomerCardLookup(string Email, CardLookup? Card);

public sealed record CardLookup(string Token, int ExpiryMonth, int ExpiryYear);

public sealed record SubmitPaymentResponse(Guid PaymentId, string Status, Guid TransactionId, Guid CorrelationId);

public sealed record ErrorResponse(string Code, string Message, Guid CorrelationId);