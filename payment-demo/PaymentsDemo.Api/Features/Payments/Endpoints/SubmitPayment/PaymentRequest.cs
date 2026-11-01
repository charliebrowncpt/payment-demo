using FastEndpoints;
using FluentValidation;

namespace PaymentsDemo.Api.Features.Payments.Endpoints.SubmitPayment;

public record PaymentIntent(
    Guid CustomerId,
    Guid CardId,
    Guid SubMerchantId,
    long Amount
);

public sealed class PaymentRequestValidator : Validator<PaymentIntent>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required.");
        RuleFor(x => x.CardId).NotEmpty().WithMessage("CardId is required.");
        RuleFor(x => x.SubMerchantId).NotEmpty().WithMessage("SubMerchantId is required.");
        RuleFor(x => x.Amount).InclusiveBetween(1, long.MaxValue).WithMessage("Amount must be a positive value.");

    }
}
