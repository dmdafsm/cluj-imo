using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace ClujImo.Web.Pages;

public sealed class DashboardModel(PermitQueries queries) : FilteredPageModel(queries)
{
    public Kpis Kpis { get; private set; } = null!;

    public LineChart Monthly { get; private set; } = null!;

    public IReadOnlyList<CategoryCount> Categories { get; private set; } = [];

    public IReadOnlyList<AreaCount> TopNeighborhoods { get; private set; } = [];

    public IReadOnlyList<AreaCount> TopStreets { get; private set; } = [];

    public IReadOnlyList<PipelineRow> Pipeline { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsPartialRequest)
        {
            await LoadFilterOptionsAsync(cancellationToken);
            if (!Request.Query.ContainsKey("an") && Years.Count > 0)
            {
                Year = Years[0];
            }
            Pipeline = await Queries.GetPipelineAsync(8, cancellationToken);
        }

        var filter = Filter;
        Kpis = await Queries.GetKpisAsync(filter, cancellationToken);
        Monthly = LineChart.From(await Queries.GetMonthlyAsync(filter, cancellationToken), Year);
        Categories = await Queries.GetCategoriesAsync(filter, cancellationToken);
        TopNeighborhoods = await Queries.GetTopNeighborhoodsAsync(filter, 10, cancellationToken);
        TopStreets = await Queries.GetTopStreetsAsync(filter, 10, cancellationToken);
        return PageOrPartial("_DashboardResults");
    }
}
