using ClujImo.Domain.Addresses;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Permits;

namespace ClujImo.Infrastructure.Ingestion;

/// <summary>Recomputes every derived column of a permit from its raw fields.</summary>
public static class PermitEnricher
{
    public static void Enrich(Permit permit, StreetDirectory streets)
    {
        var work = WorkClassifier.Classify(permit.Description);
        permit.Category = work.Category;
        permit.IsNewBuild = work.IsNewBuild;
        permit.IsContinuation = work.IsContinuation;
        permit.IsAmendment = work.IsAmendment;
        permit.IncludesDemolition = work.IncludesDemolition;
        permit.IsUrbanPlan = work.IsUrbanPlan;
        permit.HeightRegime = work.HeightRegime;
        permit.LevelsAboveGround = work.LevelsAboveGround;
        permit.Units = work.Units;

        // Completion certificates hold a street address in older years and only a neighborhood ("Zorilor") lately.
        if (permit.Kind == PermitKind.CompletionCertificate && !permit.RawAddress.Any(char.IsDigit))
        {
            permit.Neighborhood = NormalizeNeighborhood(permit.RawAddress);
            return;
        }

        var address = AddressParser.Parse(permit.RawAddress);
        var street = streets.Resolve(address.StreetKey);
        permit.StreetKey = street?.Key ?? address.StreetKey;
        permit.StreetDisplay = street?.Name ?? address.Street;
        permit.HouseNumber = address.HouseNumber;
        permit.IsZone = address.IsZone;
        permit.Neighborhood = street?.Neighborhood;
    }

    private static string? NormalizeNeighborhood(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return string.Concat(char.ToUpperInvariant(trimmed[0]), trimmed[1..]);
    }
}
