namespace PaymentsDemo.Api.Shared.Requests;

public record PaymentEnvelope<TRequest>(MessageHeader Header, TRequest Request);

public record MessageHeader(string MessageDateTime, string MessageId, string Channel);