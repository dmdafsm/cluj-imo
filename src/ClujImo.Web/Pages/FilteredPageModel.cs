using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClujImo.Web.Pages;

/// <summary>Shared query-string filters (?tip=&amp;an=&amp;grup=&amp;cartier=&amp;q=) and htmx partial detection.</summary>
public abstract class FilteredPageModel(PermitQueries queries) : PageModel
{
    protected PermitQueries Queries { get; } = queries;

    [BindProperty(SupportsGet = true, Name = "tip")]
    public PermitKind Kind { get; set; } = PermitKind.BuildingPermit;

    [BindProperty(SupportsGet = true, Name = "an")]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true, Name = "grup")]
    public CategoryGroup Group { get; set; }

    [BindProperty(SupportsGet = true, Name = "cartier")]
    public string? Neighborhood { get; set; }

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Search { get; set; }

    public IReadOnlyList<int> Years { get; private set; } = [];

    public IReadOnlyList<string> Neighborhoods { get; private set; } = [];

    public PermitFilter Filter => new()
    {
        Kind = Kind,
        Year = Year,
        Group = Group,
        Neighborhood = string.IsNullOrWhiteSpace(Neighborhood) ? null : Neighborhood,
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search,
    };

    /// <summary>True for htmx swaps; false for full loads and htmx history restores (which need the whole page).</summary>
    protected bool IsPartialRequest =>
        Request.Headers.ContainsKey("HX-Request") && !Request.Headers.ContainsKey("HX-History-Restore-Request");

    protected async Task LoadFilterOptionsAsync(CancellationToken cancellationToken)
    {
        Years = await Queries.GetYearsAsync(cancellationToken);
        Neighborhoods = await Queries.GetNeighborhoodsAsync(cancellationToken);
    }

    protected IActionResult PageOrPartial(string partialName)
    {
        Response.Headers.Vary = "HX-Request";
        return IsPartialRequest ? Partial(partialName, this) : Page();
    }
}
