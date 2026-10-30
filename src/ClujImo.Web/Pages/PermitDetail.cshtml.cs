using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClujImo.Web.Pages;

public sealed class PermitDetailModel(PermitQueries queries) : PageModel
{
    public Permit Permit { get; private set; } = null!;

    public IReadOnlyList<Permit> Completions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var permit = await queries.GetAsync(id, cancellationToken);
        if (permit is null)
        {
            return NotFound();
        }

        Permit = permit;
        Completions = await queries.GetCompletionsForAsync(permit, cancellationToken);
        return Page();
    }
}
