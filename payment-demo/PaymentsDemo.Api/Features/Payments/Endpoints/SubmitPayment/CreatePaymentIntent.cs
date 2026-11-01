using FastEndpoints;
using FluentValidation;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Features.Payments.Endpoints.SubmitPayment;

public sealed class CreatePaymentIntent
{
    [FromHeader("X-Idempotency-Key")]
    public Guid IdempotencyKey { get; init; }

    [FromHeader("X-Api-CorrelationId")]
    public Guid? CorrelationId { get; init; }

    public Guid CustomerId { get; init; }
    public Guid CardId { get; init; }
    public Guid SubMerchantId { get; init; }

    /// <summary>Per the spec --> Lowest currency unit. 1999 = R19.99.</summary>
    public long Amount { get; init; }
}

public sealed class SubmitPaymentValidator : Validator<CreatePaymentIntent>
{
    public SubmitPaymentValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .Must(IsUuidV4).WithMessage("X-Idempotency-Key must be a UUIDv4.");

        // Absent is fine and the endpoint generates one. Present must be a valid GUID.
        // An empty header counts as present, so it fails.
        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .When(x => x.CorrelationId.HasValue)
            .WithMessage("X-Api-CorrelationId must be a GUID when supplied.");

        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();

        RuleFor(x => x.SubMerchantId)
            .Must(id => SubMerchantCodeValueObject.TryCreate(id, out _))
            .WithMessage("subMerchantId must be 1-15 letters or digits.");

        RuleFor(x => x.Amount).GreaterThan(0);
    }

    // The version nibble is the 15th character of a canonical UUID.
    // Time-based UUIDs share prefixes, which would cause Acme E09 collisions on the retrieval reference.
    private static bool IsUuidV4(Guid key)
    {
        var keyString = key.ToString();
        return keyString.Length == 36 && Guid.TryParseExact(keyString, "D", out _) && keyString[14] == '4';
    }
}