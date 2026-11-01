using FluentValidation;

namespace AcmePay.Shared;

// Carried on every request and response.
public record MessageHeader(string MessageDateTime, string MessageID, string Channel);

public sealed class MessageHeaderValidator : AbstractValidator<MessageHeader>
{
    public MessageHeaderValidator()
    {
        RuleFor(x => x.MessageDateTime).NotEmpty();
        RuleFor(x => x.MessageID).NotEmpty();
        RuleFor(x => x.Channel).Must(c => c is "App" or "Web" or "POS")
            .WithMessage("Channel must be App, Web or POS.");
    }
}