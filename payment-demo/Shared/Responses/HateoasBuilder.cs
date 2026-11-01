using System.Collections.Concurrent;
using FastEndpoints;

namespace PaymentDemo.Shared.Responses;

public record Link(HrefRelation Relation, Http HttpVerb, string Href);

public enum HrefRelation
{
    Parent,
    Self,
    Blank
}

public class HateOasResponseBuilder
{
    private readonly ConcurrentDictionary<Link, byte> _links = new();

    public HateOasResponseBuilder Attach(HrefRelation relation, Http httpVerb, string href)
    {
        _ = _links.TryAdd(new Link(relation, httpVerb, href), 0);
        return this;
    }

    public IReadOnlyList<Link> Build() => [.. _links.Keys];
}