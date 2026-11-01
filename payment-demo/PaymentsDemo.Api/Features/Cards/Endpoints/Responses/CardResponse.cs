using PaymentsDemo.Api.Shared.Responses;

namespace PaymentsDemo.Api.Features.Cards.Endpoints.Responses;

public record CardResponse(Guid Id, string Token, IReadOnlyList<Link> Links);