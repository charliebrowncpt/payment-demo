using System.Net;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using PaymentsDemo.Api.Shared.Utilities;
using PaymentsDemo.Api.Shared.Responses;
using PaymentsDemo.Api.Infrastructure.Data;
using PaymentsDemo.Api.Features.Cards.Endpoints.Responses;
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;

namespace PaymentsDemo.Api.Features.Cards.Endpoints;

public record CreateCardCommand
{
    [Microsoft.AspNetCore.Mvc.FromForm] public string CardNumber { get; init; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.FromForm] public string NameOnCard { get; init; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.FromForm] public string Cvc { get; init; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.FromForm] public int ExpiresOnDay { get; private set; }
    [Microsoft.AspNetCore.Mvc.FromForm] public int ExpiresOnMonth { get; private set; }
    [Microsoft.AspNetCore.Mvc.FromForm] public int ExpiresOnYear { get; private set; }

    internal string GenerateToken()
    {
        string hash = ToString().GetHashCode().ToString();
        var key = $"{Cvc}|{ExpiresOnYear}_{NameOnCard}|_^_{ExpiresOnMonth}@{CardNumber}^_^";

        return HashGenerator.GenerateHash(hash + " (_) " + key);
    }
}

public class CreateCardRequestValidator : Validator<CreateCardCommand>
{
    public CreateCardRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard()
            .WithMessage("A valid card number is required.");

        RuleFor(x => x.NameOnCard)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("'Name on Card' is required");

        RuleFor(x => x.ExpiresOnDay)
            .NotEmpty()
            .WithMessage("Expiry date 'Day' is required")
            .InclusiveBetween(0, 31).WithMessage("Day must be smaller than 31 and greater than 0");

        RuleFor(x => x.ExpiresOnMonth)
          .NotEmpty()
          .WithMessage("Expiry date 'Month' is required")
          .InclusiveBetween(1, 12).WithMessage("Day must be smaller than 31 and greater than 0");

        RuleFor(x => x.ExpiresOnYear)
            .NotEmpty()
            .WithMessage("Expiry date 'Year' is required")
            .GreaterThanOrEqualTo(DateTimeOffset.UtcNow.Year)
            .WithMessage("Expiry date 'Year' cannot be in the past");
    }
}

file static class Mapper
{
    public static BankCard MapToCardDomainPoco(CreateCardCommand source) => new(
        cardNumber: source.CardNumber,
        nameOnCard: source.NameOnCard,
        cvc: source.Cvc,
        expiresOnDay: source.ExpiresOnDay,
        expiresOnMonth: source.ExpiresOnMonth,
        expiresOnYear: source.ExpiresOnYear,
        isActive: true,
        createdBy: Guid.Empty
    );
}

public class CreateCardEndpoint(
    ApplicationDbContext dbContext,
    LinkGenerator linkGenerator,
    HateOasResponseBuilder hateOasBuilder) : Endpoint<CreateCardCommand>
{
    public override void Configure()
    {
        Version(1);
        Post("/cards");
        AllowFormData();
    }

    public async override Task HandleAsync(CreateCardCommand req, CancellationToken ct)
    {
        bool? idempotencyKeyExists = HttpContext?.Request.Headers.TryGetValue("x-api-idempotency-key", out StringValues idempotencyKey);
        if (idempotencyKeyExists is null || !idempotencyKeyExists.Value)
        {
            await Send.StatusCodeAsync((int)HttpStatusCode.BadRequest, ct);
            return;
        }

        var cardFingerprint = req.GenerateToken();
        var dbCard = await dbContext.BankCards.FirstOrDefaultAsync(c => c.CardToken == cardFingerprint, ct);

        if (dbCard is not null)
        {
            await Send.StatusCodeAsync((int)HttpStatusCode.Conflict, ct);
            return;
        }

        var dbRecord = Mapper.MapToCardDomainPoco(req);
        await dbContext.AddAsync(dbRecord, ct);
        await dbContext.SaveChangesAsync(ct);

        var self = linkGenerator.GetUriByName(HttpContext!, "GetCardByToken", new { Token = cardFingerprint });

        var links = hateOasBuilder.Attach(relation: HrefRelation.Self, httpVerb: Http.GET, href: self!)
                                          .Attach(relation: HrefRelation.Self, httpVerb: Http.DELETE, href: self!)
                                          .Attach(relation: HrefRelation.Self, httpVerb: Http.PUT, href: $"{self}/{dbRecord.Id}")
                                          .Build();

        await Send.CreatedAtAsync("GetCardByToken", new { Token = cardFingerprint }, new CardResponse(
            dbRecord.Id,
            cardFingerprint,
            links
        ), cancellation: ct);
    }
}