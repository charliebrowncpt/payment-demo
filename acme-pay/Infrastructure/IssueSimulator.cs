using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace AcmePay.Infrastructure;

public enum HoldResult { Responded, ClientGone, DeadLettered }

// ReachedAcquirer is only meaningful when Result is not Responded.
public sealed record Hold(HoldResult Result, bool ReachedAcquirer);

public sealed record DeadLetter(string TransactionId, string Rrn, DateTime DeadLetteredUtc, bool ReachedAcquirer);

// Stands in for the issuer/acquirer round trip. Register as a singleton.
//
// Test headers (only honoured while Simulation:Enabled is true):
//   X-Simulate-Delay-Ms: 45000            force the hold time (above the TTL = dead-lettered)
//   X-Simulate-Reached-Acquirer: true     force whether a timed-out request reached the acquirer
public sealed class IssuerSimulator(IOptions<SimulationOptions> options, ILogger<IssuerSimulator> log)
{
    private readonly SimulationOptions _o = options.Value;
    private readonly ConcurrentQueue<DeadLetter> _deadLetters = new();

    public IReadOnlyCollection<DeadLetter> DeadLetters => _deadLetters;

    public async Task<Hold> HoldAsync(
        string transactionId, string rrn, IHeaderDictionary headers, CancellationToken clientAborted)
    {
        if (!_o.Enabled) return new Hold(HoldResult.Responded, false);

        var delay = PickDelay(headers);
        var reached = PickReached(headers);

        // Dead-letter TTL: whichever comes first, the issuer answering, the caller hanging up
        // (clientAborted), or the deadline.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(clientAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(_o.DeadLetterAfterSeconds));

        try
        {
            await Task.Delay(delay, deadline.Token);
            return new Hold(HoldResult.Responded, reached);
        }
        catch (OperationCanceledException)
        {
            if (clientAborted.IsCancellationRequested)
            {
                log.LogWarning("Caller gave up on {TransactionId} (reachedAcquirer={Reached})", transactionId, reached);
                return new Hold(HoldResult.ClientGone, reached);
            }

            _deadLetters.Enqueue(new DeadLetter(transactionId, rrn, DateTime.UtcNow, reached));
            log.LogWarning("Dead-lettered {TransactionId} after {Seconds}s (reachedAcquirer={Reached})",
                transactionId, _o.DeadLetterAfterSeconds, reached);
            return new Hold(HoldResult.DeadLettered, reached);
        }
    }

    private TimeSpan PickDelay(IHeaderDictionary headers)
    {
        if (headers.TryGetValue("X-Simulate-Delay-Ms", out var h) && int.TryParse(h, out var forced) && forced >= 0)
            return TimeSpan.FromMilliseconds(forced);

        var roll = Random.Shared.NextDouble();
        if (roll < _o.UnresponsiveProbability) return Timeout.InfiniteTimeSpan;   // held until the TTL fires
        if (roll < _o.UnresponsiveProbability + _o.SlowProbability)
            return TimeSpan.FromMilliseconds(Random.Shared.Next(_o.SlowMinMs, _o.SlowMaxMs + 1));
        return TimeSpan.FromMilliseconds(Random.Shared.Next(_o.FastMinMs, _o.FastMaxMs + 1));
    }

    private bool PickReached(IHeaderDictionary headers) =>
        headers.TryGetValue("X-Simulate-Reached-Acquirer", out var h) && bool.TryParse(h, out var forced)
            ? forced
            : Random.Shared.NextDouble() < _o.ReachedAcquirerProbability;
}

// Bound from the "Simulation" section of appsettings.json.
public sealed class SimulationOptions
{
    public bool Enabled { get; set; } = true;

    // Most requests: answered in under 400 ms.
    public int FastMinMs { get; set; } = 50;
    public int FastMaxMs { get; set; } = 400;

    // An unresponsive issuer holds a request for up to 30 s. Kept just under the dead-letter
    // TTL so a slow hold still gets answered rather than racing the deadline.
    public double SlowProbability { get; set; } = 0.05;
    public int SlowMinMs { get; set; } = 1_000;
    public int SlowMaxMs { get; set; } = 29_500;

    // The issuer never answers: the request sits until the dead-letter TTL fires.
    public double UnresponsiveProbability { get; set; } = 0.01;

    // A request still unanswered after this long is dead-lettered and answered 504 / E50.
    public int DeadLetterAfterSeconds { get; set; } = 30;

    // When a request times out (caller hangs up, or it is dead-lettered), the chance it had
    // already reached the acquirer. 0.5 = "may or may not have".
    public double ReachedAcquirerProbability { get; set; } = 0.5;
}