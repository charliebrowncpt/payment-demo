using PaymentDemo.Shared.Responses;

namespace PaymentDemo.Cards.Endpoints.Responses;

public record CardResponse(Guid Id, string Token, IReadOnlyList<Link> Links);