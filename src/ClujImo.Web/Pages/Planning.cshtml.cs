using ClujImo.Domain.Planning;
using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClujImo.Web.Pages;

public sealed class PlanningModel(InsightQueries queries) : PageModel
{
    private const int PageSize = 20;

    [BindProperty(SupportsGet = true, Name = "etapa")]
    public UrbanPlanStage? Stage { get; set; }

    [BindProperty(SupportsGet = true, Name = "tip")]
    public UrbanPlanKind? Kind { get; set; }

    [BindProperty(SupportsGet = true, Name = "an")]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true, Name = "cartier")]
    public string? Neighborhood { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int PageNumber { get; set; } = 1;

    public Page<PlanRow> Plans { get; private set; } = null!;

    public IReadOnlyList<int> Years { get; private set; } = [];

    public ColumnChart Intents { get; private set; } = null!;

    public ColumnChart Approvals { get; private set; } = null!;

    public IReadOnlyList<NamedTotal> Neighborhoods { get; private set; } = [];

    public (int Puz, int Pud, int Approved) LastYear { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Plans = await queries.GetPlansAsync(Stage, Kind, Year, Neighborhood, PageNumber, PageSize, cancellationToken);
        Response.Headers.Vary = "HX-Request";
        if (Request.Headers.ContainsKey("HX-Request") && !Request.Headers.ContainsKey("HX-History-Restore-Request"))
        {
            return Partial("_PlanningResults", this);
        }

        Years = await queries.GetPlanYearsAsync(cancellationToken);
        var intents = await queries.GetPlanMonthsAsync(UrbanPlanStage.Intent, cancellationToken);
        var approvals = await queries.GetPlanMonthsAsync(UrbanPlanStage.Approved, cancellationToken);
        Intents = ColumnChart.From(intents, 36, "PUZ", "PUD");
        Approvals = ColumnChart.From(approvals, 36, "PUZ", "PUD");
        Neighborhoods = await queries.GetPlanNeighborhoodsAsync(12, cancellationToken);

        var now = DateTime.UtcNow;
        var since = (now.Year * 12) + now.Month - 12;
        bool Recent(MonthCount m) => (m.Year * 12) + m.Month > since;
        LastYear = (intents.Where(Recent).Sum(m => m.First), intents.Where(Recent).Sum(m => m.Second), approvals.Where(Recent).Sum(m => m.First + m.Second));
        return Page();
    }

    public string PageUrl(int page)
    {
        var query = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        query["pagina"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Request.Path + QueryString.Create(query!).ToUriComponent();
    }
}
