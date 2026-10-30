using System.Text.Json;
using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace ClujImo.Web.Pages;

public sealed class MapModel(PermitQueries queries) : FilteredPageModel(queries)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public IReadOnlyList<MapPoint> Points { get; private set; } = [];

    public string PointsJson => JsonSerializer.Serialize(
        Points.Select(p => new
        {
            p.Street,
            Lat = p.Latitude,
            Lng = p.Longitude,
            p.Count,
            p.NewBuilds,
            p.Units,
            Category = Labels.For(p.TopCategory),
            p.Neighborhood,
        }),
        Json);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsPartialRequest)
        {
            await LoadFilterOptionsAsync(cancellationToken);
            if (!Request.Query.ContainsKey("an") && Years.Count > 0)
            {
                Year = Years[0];
            }
        }

        Points = await Queries.GetMapPointsAsync(Filter, cancellationToken);
        return PageOrPartial("_MapResults");
    }
}
