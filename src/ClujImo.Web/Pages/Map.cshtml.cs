using System.Text.Json;
using ClujImo.Domain.Classification;
using ClujImo.Domain.Geo;
using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace ClujImo.Web.Pages;

public sealed class MapModel(PermitQueries queries, InsightQueries insights) : FilteredPageModel(queries)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public IReadOnlyList<MapCluster> Clusters { get; private set; } = [];

    public string ClustersJson => JsonSerializer.Serialize(
        Clusters.Select(c => new
        {
            Lat = c.Latitude,
            Lng = c.Longitude,
            c.Count,
            c.NewBuilds,
            c.Units,
            c.Label,
            Exact = c.Precision >= GeoPrecision.NearbyNumber,
            Precision = Labels.For(c.Precision),
            Category = Labels.For(c.TopCategory),
            Items = c.Items.Select(i => new { i.Id, i.Title, Category = Labels.For(Enum.Parse<WorkCategory>(i.Kind)) }),
        }),
        Json);

    public string LayersJson { get; private set; } = "{}";

    public int ExactCount => Clusters.Where(c => c.Precision >= GeoPrecision.NearbyNumber).Sum(c => c.Count);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsPartialRequest)
        {
            await LoadFilterOptionsAsync(cancellationToken);
            if (!Request.Query.ContainsKey("an") && Years.Count > 0)
            {
                Year = Years[0];
            }

            var plans = await insights.GetPlanPointsAsync(24, cancellationToken);
            var sites = await insights.GetConstructionSitesAsync(cancellationToken);
            LayersJson = JsonSerializer.Serialize(new
            {
                Plans = plans.Select(p => new
                {
                    Lat = p.Latitude,
                    Lng = p.Longitude,
                    Kind = Labels.For(p.Kind),
                    Stage = Labels.For(p.Stage),
                    p.Title,
                    Date = Format.Date(p.Date),
                    p.Url,
                }),
                Sites = sites.Select(s => new { Lat = s.Latitude, Lng = s.Longitude, s.Name, s.Building }),
            }, Json);
        }

        Clusters = await Queries.GetMapClustersAsync(Filter, cancellationToken);
        return PageOrPartial("_MapResults");
    }
}
