using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentDemo.Cards.Endpoints.Responses;
using PaymentDemo.Infrastructure.Data;
using PaymentDemo.Infrastructure.Domain;
using PaymentDemo.Infrastructure.Extensions;
using PaymentDemo.Shared.Responses;

namespace PaymentDemo.Cards.Endpoints;

public record GetCardByTokenRequest([FromRoute] string Token);

file static class Mapper
{
    public static CardResponse MapToCardResponse (CardDetail source, IReadOnlyList<Link> links) => new(
        source.Id,
        source.CardToken!,
        links
    );
}

public class GetCardByTokenEndpoint(
    ApplicationDbContext dbContext,
    // IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator,
    HateOasResponseBuilder hateOasBuilder) : Endpoint<GetCardByTokenRequest>
{
    public override void Configure()
    {
        Version(1);
        Get("/cards/{token}");
        
        Description(b => b.WithName("GetCardByToken"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetCardByTokenRequest req, CancellationToken ct)
    {
        var dbRecord = await dbContext.CardDetails.SingleOrDefaultAsync(c => c.CardToken == req.Token, ct)
                                                  .ThrowsNotFoundIfNull();

        var self = linkGenerator.GetUriByName(HttpContext!, "GetCardByToken", new { Token = dbRecord!.CardToken });

        var links = hateOasBuilder.Attach(relation: HrefRelation.Self, httpVerb: Http.GET, href: self!)
                                          .Attach(relation: HrefRelation.Self, httpVerb: Http.DELETE, href: self!)
                                          .Attach(relation: HrefRelation.Self, httpVerb: Http.PUT, href: self!)
                                          .Build();

        await Send.OkAsync(Mapper.MapToCardResponse(dbRecord, links), ct);

    }
}