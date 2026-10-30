using ClujImo.Domain.Addresses;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Geo;
using ClujImo.Domain.Permits;
using ClujImo.Domain.Planning;

namespace ClujImo.Infrastructure.Ingestion;

/// <summary>Recomputes every derived column from raw fields.</summary>
public static class PermitEnricher
{
    public static void Enrich(Permit permit, GeoContext geo)
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
            (permit.Latitude, permit.Longitude, permit.GeoPrecision) = (null, null, GeoPrecision.None);
            return;
        }

        var address = AddressParser.Parse(permit.RawAddress);
        var placement = geo.Place(address);
        permit.StreetKey = placement.StreetKey;
        permit.StreetDisplay = placement.StreetDisplay;
        permit.HouseNumber = address.HouseNumber;
        permit.IsZone = address.IsZone;
        permit.Neighborhood = placement.Neighborhood;
        permit.Latitude = placement.Point?.Latitude;
        permit.Longitude = placement.Point?.Longitude;
        permit.GeoPrecision = placement.Precision;
    }

    public static void Enrich(UrbanPlan plan, GeoContext geo)
    {
        plan.Category = WorkClassifier.Classify(plan.Title).Category;
        var address = FreeTextAddress.Find(plan.Title);
        var placement = geo.Place(address);
        plan.StreetKey = placement.StreetKey;
        plan.StreetDisplay = placement.StreetDisplay;
        plan.HouseNumber = address?.HouseNumber;
        plan.Neighborhood = placement.Neighborhood;
        plan.Latitude = placement.Point?.Latitude;
        plan.Longitude = placement.Point?.Longitude;
        plan.GeoPrecision = placement.Precision;
    }

    private static string? NormalizeNeighborhood(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : string.Concat(char.ToUpperInvariant(trimmed[0]), trimmed[1..]);
    }
}
