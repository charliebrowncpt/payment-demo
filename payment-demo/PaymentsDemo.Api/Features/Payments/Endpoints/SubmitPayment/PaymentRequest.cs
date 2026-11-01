using FastEndpoints;
using FluentValidation;

namespace PaymentDemo.Features.Payments.Endpoints.SubmitPayment;

// Acme uses PascalCase on the payment API and camelCase on the Transaction Status
// webhook; FastEndpoints reads both case-insensitively. Unknown fields are ignored.

public record Card(string Token);

public record PaymentRequest(
    string MerchantId,
    string TransactionId,
    string OrderNumber,
    long Amount,
    string PaymentService,             // CardNotPresent | CardNotPresentRecurring | AcmeEft | GooglePay | ApplePay
    Card? Card = null,
    string? CurrencyCode = null,       // optional, Acme defaults to ZAR
    string? CardOnFileScenario = null, // CIT | MIT
    string? SubMerchantId = null,
    string? MerchantCategoryCode = null,
    string? CardAcceptorName = null,
    string? Email = null
);

public sealed class PaymentRequestValidator : Validator<PaymentRequest>
{
    private static readonly string[] Services =
        ["CardNotPresent", "CardNotPresentRecurring", "AcmeEft", "GooglePay", "ApplePay"];

    private static bool IsCardService(string? s) =>
        s is "CardNotPresent" or "CardNotPresentRecurring" or "GooglePay" or "ApplePay";

    public PaymentRequestValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TransactionId).NotEmpty().MaximumLength(50);

        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9]+$")
            .WithMessage("OrderNumber must be alphanumeric only.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount is in the lowest monetary unit (1999 = R19.99).");

        RuleFor(x => x.CurrencyCode)
            .Length(3)
            .When(x => x.CurrencyCode is not null);

        RuleFor(x => x.PaymentService)
            .Must(s => Services.Contains(s))
            .WithMessage("Unknown PaymentService.");

        RuleFor(x => x.Card)
            .Must(c => !string.IsNullOrEmpty(c?.Token))
            .When(x => IsCardService(x.PaymentService))
            .WithMessage("Card.Token is required on card services.");

        RuleFor(x => x.CardOnFileScenario)
            .Must(s => s is null or "CIT" or "MIT")
            .WithMessage("CardOnFileScenario must be CIT or MIT.");

        RuleFor(x => x.Email).MaximumLength(254);

        // Payment facilitator fields
        RuleFor(x => x.SubMerchantId).MaximumLength(15);

        RuleFor(x => x.MerchantCategoryCode)
            .Length(4)
            .When(x => x.MerchantCategoryCode is not null);

        RuleFor(x => x.CardAcceptorName).Length(40)
            .When(x => x.CardAcceptorName is not null)
            .WithMessage("CardAcceptorName must be exactly 40 characters, space-padded.");

        RuleFor(x => x.CardAcceptorName)
            .Must(HasValidAsteriskPosition)
            .When(x => x.CardAcceptorName is { Length: 40 })
            .WithMessage("'*' may only appear at position 4, 8 or 13.");
    }

    // ABSA/FNB/Nedbank: FacilitatorName*SubMerchantName. Standard Bank has no '*'.
    private static bool HasValidAsteriskPosition(string? name)
    {
        int star = name![..23].IndexOf('*');
        return star < 0 || star + 1 is 4 or 8 or 13;
    }
}
