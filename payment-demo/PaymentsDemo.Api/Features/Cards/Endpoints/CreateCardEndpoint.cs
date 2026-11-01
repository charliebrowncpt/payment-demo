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
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Enums;

namespace PaymentsDemo.Api.Features.Cards.Endpoints;

public record CreateCardCommand
{
    [Microsoft.AspNetCore.Mvc.FromForm] public Guid CustomerId { get; init; }
    [Microsoft.AspNetCore.Mvc.FromForm] public string CardNumber { get; init; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.FromForm] public string NameOnCard { get; init; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.FromForm] public string Cvc { get; init; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.FromForm] public int ExpiryDay { get; private set; }
    [Microsoft.AspNetCore.Mvc.FromForm] public int ExpiryMonth { get; private set; }
    [Microsoft.AspNetCore.Mvc.FromForm] public int ExpiryYear { get; private set; }

    internal string GenerateToken()
    {
        string hash = $"{Cvc}|{ExpiryYear}_{NameOnCard}|_^_{ExpiryMonth}@{CardNumber}^_^";
        return HashGenerator.GenerateHash(hash + " (_) ");
    }
}

public class CreateCardRequestValidator : Validator<CreateCardCommand>
{
    public CreateCardRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer Id is required.");

        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .CreditCard()
            .WithMessage("A valid card number is required.");

        RuleFor(x => x.NameOnCard)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("'Name on Card' is required");

        RuleFor(x => x.ExpiryDay)
            .NotEmpty()
            .WithMessage("Expiry date 'Day' is required")
            .InclusiveBetween(0, 31).WithMessage("Day must be smaller than 31 and greater than 0");

        RuleFor(x => x.ExpiryMonth)
          .NotEmpty()
          .WithMessage("Expiry date 'Month' is required")
          .InclusiveBetween(1, 12).WithMessage("Day must be smaller than 31 and greater than 0");

        RuleFor(x => x.ExpiryYear)
            .NotEmpty()
            .WithMessage("Expiry date 'Year' is required")
            .GreaterThanOrEqualTo(DateTimeOffset.UtcNow.Year)
            .WithMessage("Expiry date 'Year' cannot be in the past");
    }
}

file static class Mapper
{
    public static BankCard MapToCardDomainPoco(CreateCardCommand source) => new(
        customerId: source.CustomerId,
        cardNumber: source.CardNumber,
        nameOnCard: source.NameOnCard,
        cvc: source.Cvc,
        ExpiryDay: source.ExpiryDay,
        ExpiryMonth: source.ExpiryMonth,
        ExpiryYear: source.ExpiryYear,
        cardStatus: BankCardStatus.Active,
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