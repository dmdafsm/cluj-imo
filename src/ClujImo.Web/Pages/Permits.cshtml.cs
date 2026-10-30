using ClujImo.Infrastructure.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ClujImo.Web.Pages;

public sealed class PermitsModel(PermitQueries queries) : FilteredPageModel(queries)
{
    private const int PageSize = 25;

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int PageNumber { get; set; } = 1;

    public Page<PermitRow> Results { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsPartialRequest)
        {
            await LoadFilterOptionsAsync(cancellationToken);
        }

        Results = await Queries.SearchAsync(Filter, PageNumber, PageSize, cancellationToken);
        return PageOrPartial("_PermitsResults");
    }

    public string PageUrl(int page)
    {
        var query = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        query["pagina"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Request.Path + Microsoft.AspNetCore.Http.QueryString.Create(query!).ToUriComponent();
    }
}
