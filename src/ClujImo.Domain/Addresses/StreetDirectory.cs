namespace ClujImo.Domain.Addresses;

/// <summary>Resolves street keys written in many ways ("G.ASACHI", "Str Gheorghe Asachi") to nomenclator streets.</summary>
public sealed class StreetDirectory
{
    private readonly Dictionary<string, Street> _byKey;
    private readonly ILookup<string, Street> _byLastToken;

    public StreetDirectory(IEnumerable<Street> streets)
    {
        _byKey = new Dictionary<string, Street>(StringComparer.Ordinal);
        foreach (var street in streets)
        {
            _byKey.TryAdd(street.Key, street);
        }

        _byLastToken = _byKey.Values.ToLookup(s => s.Key.Split(' ')[^1], StringComparer.Ordinal);
    }

    public int Count => _byKey.Count;

    public Street? Resolve(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (_byKey.TryGetValue(key, out var exact))
        {
            return exact;
        }

        // Fuzzy: same last token, and every input token equals or abbreviates (initial) a candidate token.
        var tokens = key.Split(' ');
        var candidates = _byLastToken[tokens[^1]]
            .Where(s => Covers(s.Key.Split(' '), tokens))
            .Take(2)
            .ToList();

        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static bool Covers(string[] candidate, string[] input) =>
        input.All(token => candidate.Any(c => c == token || (token.Length == 1 && c.StartsWith(token, StringComparison.Ordinal))));
}
