using ClujImo.Infrastructure.Geocoding;
using ClujImo.Infrastructure.Ingestion;
using ClujImo.Infrastructure.Ingestion.Sources;
using ClujImo.Infrastructure.Persistence;
using ClujImo.Infrastructure.Queries;
using ClujImo.Infrastructure.Sources;
using ClujImo.Infrastructure.Sources.CityHall;
using ClujImo.Infrastructure.Sources.Ins;
using ClujImo.Infrastructure.Sources.Osm;
using ClujImo.Infrastructure.Sources.Sicap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClujImo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddOptions<OpenDataOptions>(services, configuration, OpenDataOptions.Section);
        AddOptions<GeocodingOptions>(services, configuration, GeocodingOptions.Section);
        AddOptions<OsmOptions>(services, configuration, OsmOptions.Section);
        AddOptions<InsOptions>(services, configuration, InsOptions.Section);
        AddOptions<CityHallOptions>(services, configuration, CityHallOptions.Section);
        AddOptions<InvestmentOptions>(services, configuration, InvestmentOptions.Section);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=cluj-imo.db"));

        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient<CkanClient>((sp, http) =>
            {
                http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OpenDataOptions>>().Value.CkanApiUrl);
                http.Timeout = TimeSpan.FromMinutes(2);
            })
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
            });

        services.AddHttpClient<NominatimGeocoder>((sp, http) =>
            {
                http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<GeocodingOptions>>().Value.NominatimUrl);
                Identify(sp, http);
                http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ro");
            });

        services.AddHttpClient<OverpassClient>((sp, http) =>
        {
            Identify(sp, http);
            http.Timeout = TimeSpan.FromMinutes(4);
        });

        services.AddHttpClient<TempoClient>((sp, http) =>
        {
            http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<InsOptions>>().Value.TempoUrl);
            Identify(sp, http);
            http.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddHttpClient<LargeFileDownloader>((sp, http) =>
        {
            Identify(sp, http);
            http.Timeout = TimeSpan.FromMinutes(30);
        });

        services.AddHttpClient<PlanningIngester>((sp, http) =>
        {
            Identify(sp, http);
            http.Timeout = TimeSpan.FromMinutes(2);
        });

        // Order matters: OSM before enrichment-heavy sources is irrelevant (enrichment runs last), but permits first
        // keeps the core data fresh even if a slow source fails later.
        services.AddScoped<PermitsIngester>();
        services.AddScoped<OsmIngester>();
        services.AddScoped<InsIngester>();
        services.AddScoped<PaymentsIngester>();
        services.AddScoped<SicapIngester>();
        services.AddScoped<ISourceIngester>(sp => sp.GetRequiredService<PermitsIngester>());
        services.AddScoped<ISourceIngester>(sp => sp.GetRequiredService<OsmIngester>());
        services.AddScoped<ISourceIngester>(sp => sp.GetRequiredService<PlanningIngester>());
        services.AddScoped<ISourceIngester>(sp => sp.GetRequiredService<InsIngester>());
        services.AddScoped<ISourceIngester>(sp => sp.GetRequiredService<PaymentsIngester>());
        services.AddScoped<ISourceIngester>(sp => sp.GetRequiredService<SicapIngester>());

        services.AddScoped<IngestionService>();
        services.AddScoped<GeocodingService>();
        services.AddScoped<PermitQueries>();
        services.AddScoped<InsightQueries>();
        return services;
    }

    private static void AddOptions<T>(IServiceCollection services, IConfiguration configuration, string section)
        where T : class =>
        services.AddOptions<T>().Bind(configuration.GetSection(section)).ValidateDataAnnotations().ValidateOnStart();

    /// <summary>Every outbound request identifies the app (required by the Nominatim and Overpass usage policies).</summary>
    private static void Identify(IServiceProvider sp, HttpClient http) =>
        http.DefaultRequestHeaders.UserAgent.ParseAdd(sp.GetRequiredService<IOptions<GeocodingOptions>>().Value.UserAgent);
}
