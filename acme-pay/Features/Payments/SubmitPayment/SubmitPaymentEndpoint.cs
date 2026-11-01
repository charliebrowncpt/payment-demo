using System.Globalization;
using AcmePay.Infrastructure;
using AcmePay.Shared;
using FastEndpoints;

namespace AcmePay.Features.Payments.SubmitPayment;

// POST /api/payment: submits a payment for processing.
public sealed class SubmitPaymentEndpoint(InMemoryPaymentStore store) 
    : Endpoint<PaymentRequestEnvelope, PaymentResponseEnvelope>
{
    public override void Configure()
    {
        Post("/api/payment");
        AllowAnonymous();                                          // API-key auth is the filter below,
        Options(o => o.AddEndpointFilter(ApiKeyFilter.InvokeAsync)); // so a bad key gets 401 before any 400
    }

    public override async Task HandleAsync(PaymentRequestEnvelope req, CancellationToken ct)
    {
        var p = req.PaymentRequest;

        // The first 12 characters of OrderNumber form the Retrieval Reference Number,
        // which must be unique across the merchant's account.
        var rrn = p.OrderNumber.Length > 12 ? p.OrderNumber[..12] : p.OrderNumber;

        var (outcome, response, holder) = store.Submit(p.TransactionID, rrn, () => Approve(req, rrn));

        if (outcome == StoreOutcome.RrnInUse)
        {
            // 409 / E09: the body carries the TransactionID that already holds the RRN.
            var conflict = new PaymentResponse(
                new ResponseDetail("E09", "Application", "Retrieval Reference Number already in use",
                                   "That order reference has already been used."),
                TransactionID: holder);

            await Send.ResponseAsync(PaymentResponseEnvelope.For(conflict, req.MessageHeader.Channel), 409, ct);
            return;
        }

        // Created, or Replayed: a repeated TransactionID within 24 hours gets the stored outcome.
        await Send.ResponseAsync(PaymentResponseEnvelope.For(response!, req.MessageHeader.Channel), 200, ct);
    }

    // Stub acquirer: approves everything. AcmeEft is a collection, so it stays pending.
    private static PaymentResponse Approve(PaymentRequestEnvelope req, string rrn)
    {
        var p = req.PaymentRequest;
        bool eft = p.PaymentService == "AcmeEft";

        // TransactionDateTime is when the transaction was submitted: the request's MessageDateTime.
        var when = DateTime.TryParse(req.MessageHeader.MessageDateTime, CultureInfo.InvariantCulture,
                                     DateTimeStyles.None, out var t)
            ? DateTime.SpecifyKind(t, DateTimeKind.Unspecified)
            : Sast.Now();

        var detail = eft
            ? new ResponseDetail("PEND", "EFT", "EFT collection pending", "We are waiting for your bank.")
            : new ResponseDetail("00", "Acquirer", "Approved or completed successfully", "Approved.");

        return new PaymentResponse(
            detail,
            TransactionID: p.TransactionID,
            TransactionDateTime: when,
            OrderNumber: p.OrderNumber,
            ReconID: $"MR{Random.Shared.NextInt64(100_000_000, 1_000_000_000)}",
            RetrievalReferenceNumber: rrn,
            AuthCode: eft ? null : Random.Shared.Next(100_000, 1_000_000).ToString(),
            TransactionStatus: "Success",
            AuthAmount: eft ? null : p.Amount
        );
    }
}