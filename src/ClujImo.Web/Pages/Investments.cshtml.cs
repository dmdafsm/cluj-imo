using ClujImo.Infrastructure.Queries;
using ClujImo.Web.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClujImo.Web.Pages;

public sealed class InvestmentsModel(InsightQueries queries) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "an")]
    public int? Year { get; set; }

    public IReadOnlyList<int> Years { get; private set; } = [];

    public IReadOnlyList<YearTotal> ContractYears { get; private set; } = [];

    public IReadOnlyList<NamedTotal> Contractors { get; private set; } = [];

    public IReadOnlyList<NamedTotal> WorkTypes { get; private set; } = [];

    public IReadOnlyList<ContractRow> Largest { get; private set; } = [];

    public IReadOnlyList<NamedTotal> PaymentBeneficiaries { get; private set; } = [];

    public ColumnChart? Payments { get; private set; }

    public IReadOnlyList<DecisionRow> Decisions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Years = await queries.GetInvestmentYearsAsync(cancellationToken);
        if (!Request.Query.ContainsKey("an") && Years.Count > 0)
        {
            Year = Years[0];
        }

        Contractors = await queries.GetTopContractorsAsync(Year, cancellationToken);
        WorkTypes = await queries.GetWorkTypesAsync(Year, cancellationToken);
        Largest = await queries.GetLargestContractsAsync(Year, 10, cancellationToken);
        PaymentBeneficiaries = await queries.GetTopWorksBeneficiariesAsync(Year, cancellationToken);

        Response.Headers.Vary = "HX-Request";
        if (Request.Headers.ContainsKey("HX-Request") && !Request.Headers.ContainsKey("HX-History-Restore-Request"))
        {
            return Partial("_InvestmentsResults", this);
        }

        ContractYears = await queries.GetContractYearsAsync(worksOnly: true, cancellationToken);
        var months = await queries.GetWorksPaymentMonthsAsync(cancellationToken);
        Payments = months.Count > 0 ? ColumnChart.From(months, 48, "Plăți pentru lucrări", null, " lei", multiplier: 1000) : null;
        Decisions = await queries.GetInvestmentDecisionsAsync(15, cancellationToken);
        return Page();
    }
}
