using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ClujImo.Domain.Investments;
using ClujImo.Domain.Parties;
using ClujImo.Domain.Permits;
using ClujImo.Domain.Text;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Sources;
using ClujImo.Infrastructure.Sources.Sicap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure.Ingestion.Sources;

/// <summary>The city hall's payments (data.gov.ro, one JSON per year). Each year is replaced as a whole.</summary>
public sealed class PaymentsIngester(AppDbContext db, CkanClient ckan, IOptions<InvestmentOptions> options) : ISourceIngester
{
    public string Name => "payments";

    public bool Enabled => options.Value.Enabled;

    public TimeSpan RefreshInterval => options.Value.PaymentsRefreshInterval;

    public async Task IngestAsync(IngestionRun run, CancellationToken cancellationToken)
    {
        var resources = await ckan.GetResourcesAsync(options.Value.PaymentsDataset, cancellationToken).ConfigureAwait(false);
        foreach (var resource in resources.Where(r => r.Year is not null))
        {
            var payments = Map(LenientJson.ReadRows(await ckan.DownloadAsync(resource.Url, cancellationToken).ConfigureAwait(false)));
            var year = resource.Year!.Value;
            var from = new DateOnly(year, 1, 1);
            var to = new DateOnly(year, 12, 31);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await db.PublicPayments.Where(p => p.Date >= from && p.Date <= to).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            db.PublicPayments.AddRange(payments.Where(p => p.Date >= from && p.Date <= to));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            db.ChangeTracker.Clear();

            run.RowsRead += payments.Count;
            run.RowsInserted += payments.Count;
        }
    }

    public static IReadOnlyList<PublicPayment> Map(IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
    {
        var payments = new List<PublicPayment>(rows.Count);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var date = SourceDate.Parse(row.GetValueOrDefault("DATA_EMITERII"));
            if (date is null || !decimal.TryParse(row.GetValueOrDefault("SUMA"), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                continue;
            }

            var beneficiary = row.GetValueOrDefault("BENEFICIAR");
            var description = TextNormalizer.Clean(row.GetValueOrDefault("REPREZENTAND"));

            // Identical payments on the same day exist: the occurrence index keeps their keys distinct and stable.
            var raw = string.Join('|', date.Value.ToString("O", CultureInfo.InvariantCulture), beneficiary, amount.ToString(CultureInfo.InvariantCulture), description);
            var occurrence = seen[raw] = seen.GetValueOrDefault(raw) + 1;
            payments.Add(new PublicPayment
            {
                Date = date.Value,
                Beneficiary = PartyName.Display(beneficiary),
                Amount = amount,
                Description = description,
                IsWorks = PaymentClassifier.IsWorks(description),
                RowKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{raw}|{occurrence}")))[..32],
            });
        }

        return payments;
    }
}
