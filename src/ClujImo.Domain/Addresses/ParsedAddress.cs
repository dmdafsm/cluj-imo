namespace ClujImo.Domain.Addresses;

/// <param name="Street">Street as written in the source, without the "Str"/"zona" prefixes.</param>
/// <param name="IsZone">The source only says "zona str. X": the work is near, not on, that street.</param>
public sealed record ParsedAddress(string? Street, string? StreetKey, string? HouseNumber, bool IsZone);
