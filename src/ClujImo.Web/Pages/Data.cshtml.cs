using ClujImo.Infrastructure.Ingestion;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Workers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClujImo.Web.Pages;

public sealed class DataModel(PermitQueries queries, IngestionTrigger trigger) : PageModel
{
    public IngestionRun? LastRun { get; private set; }

    public bool IsRunning { get; private set; }

    public (int Total, int Found, int Pending) Geocoding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Request.Headers.ContainsKey("HX-Request") ? Partial("_DataStatus", this) : Page();
    }

    public async Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
    {
        trigger.Request();
        await LoadAsync(cancellationToken);
        IsRunning = true;
        return Partial("_DataStatus", this);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        LastRun = await queries.GetLastRunAsync(cancellationToken);
        IsRunning = IngestionService.IsRunning;
        Geocoding = await queries.GetGeocodeStatusAsync(cancellationToken);
    }
}
