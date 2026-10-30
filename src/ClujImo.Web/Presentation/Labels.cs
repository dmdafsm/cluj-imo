using ClujImo.Domain.Classification;
using ClujImo.Domain.Permits;
using ClujImo.Infrastructure.Queries;

namespace ClujImo.Web.Presentation;

/// <summary>Romanian user-facing names for domain values.</summary>
public static class Labels
{
    public static string For(WorkCategory category) => category switch
    {
        WorkCategory.NewCollectiveResidential => "Locuințe colective noi",
        WorkCategory.NewSemiCollectiveResidential => "Locuințe semicolective",
        WorkCategory.NewSingleFamily => "Case unifamiliale",
        WorkCategory.NewMixedUse => "Clădiri mixte",
        WorkCategory.NewOffice => "Birouri",
        WorkCategory.NewCommercial => "Comerț și servicii",
        WorkCategory.NewIndustrial => "Hale și depozite",
        WorkCategory.NewPublic => "Clădiri publice",
        WorkCategory.NewOtherBuilding => "Alte clădiri noi",
        WorkCategory.Extension => "Extinderi și mansardări",
        WorkCategory.Renovation => "Renovări și modificări",
        WorkCategory.Demolition => "Desființări",
        WorkCategory.Infrastructure => "Rețele și infrastructură",
        WorkCategory.Signage => "Firme și reclame",
        WorkCategory.SiteWorks => "Împrejmuiri și amenajări",
        WorkCategory.Administrative => "Scopuri administrative",
        _ => "Altele",
    };

    public static string Icon(WorkCategory category) => category switch
    {
        WorkCategory.NewCollectiveResidential => "apartment",
        WorkCategory.NewSemiCollectiveResidential => "holiday_village",
        WorkCategory.NewSingleFamily => "house",
        WorkCategory.NewMixedUse => "domain",
        WorkCategory.NewOffice => "business_center",
        WorkCategory.NewCommercial => "storefront",
        WorkCategory.NewIndustrial => "warehouse",
        WorkCategory.NewPublic => "account_balance",
        WorkCategory.NewOtherBuilding => "foundation",
        WorkCategory.Extension => "add_home",
        WorkCategory.Renovation => "handyman",
        WorkCategory.Demolition => "delete_forever",
        WorkCategory.Infrastructure => "cable",
        WorkCategory.Signage => "signpost",
        WorkCategory.SiteWorks => "fence",
        WorkCategory.Administrative => "description",
        _ => "more_horiz",
    };

    public static string For(PermitKind kind) => kind switch
    {
        PermitKind.BuildingPermit => "Autorizații de construire",
        PermitKind.UrbanismCertificate => "Certificate de urbanism",
        PermitKind.CompletionCertificate => "Certificate de atestare",
        _ => kind.ToString(),
    };

    /// <summary>Compact name for selects and chips.</summary>
    public static string Compact(PermitKind kind) => kind switch
    {
        PermitKind.BuildingPermit => "Autorizații (AC)",
        PermitKind.UrbanismCertificate => "Cert. urbanism (CU)",
        PermitKind.CompletionCertificate => "Atestări finalizare",
        _ => kind.ToString(),
    };

    public static string Abbreviation(PermitKind kind) => kind switch
    {
        PermitKind.BuildingPermit => "AC",
        PermitKind.UrbanismCertificate => "CU",
        PermitKind.CompletionCertificate => "Atestare",
        _ => kind.ToString(),
    };

    public static string For(CategoryGroup group) => group switch
    {
        CategoryGroup.All => "Toate lucrările",
        CategoryGroup.NewBuilds => "Doar construcții noi",
        CategoryGroup.Collective => "Locuințe colective",
        CategoryGroup.SingleFamily => "Case unifamiliale",
        CategoryGroup.NonResidential => "Nerezidențial nou",
        CategoryGroup.Interventions => "Extinderi și renovări",
        CategoryGroup.Demolitions => "Cu desființare",
        _ => group.ToString(),
    };

    public static string For(ClujImo.Domain.Planning.UrbanPlanStage stage) => stage switch
    {
        ClujImo.Domain.Planning.UrbanPlanStage.Intent => "Anunț de intenție",
        ClujImo.Domain.Planning.UrbanPlanStage.Approved => "Aprobat HCL",
        _ => stage.ToString(),
    };

    public static string For(ClujImo.Domain.Planning.UrbanPlanKind kind) => kind switch
    {
        ClujImo.Domain.Planning.UrbanPlanKind.Puz => "PUZ",
        ClujImo.Domain.Planning.UrbanPlanKind.Pud => "PUD",
        _ => kind.ToString(),
    };

    public static string For(ClujImo.Domain.Statistics.MetroIndicator indicator) => indicator switch
    {
        ClujImo.Domain.Statistics.MetroIndicator.DwellingsCompleted => "Locuințe terminate",
        ClujImo.Domain.Statistics.MetroIndicator.ResidentialPermits => "Autorizații pentru clădiri rezidențiale",
        ClujImo.Domain.Statistics.MetroIndicator.ResidentialPermitArea => "Suprafață utilă autorizată (m²)",
        ClujImo.Domain.Statistics.MetroIndicator.DwellingStock => "Fond locativ (locuințe existente)",
        _ => indicator.ToString(),
    };

    public static string Source(string source) => source switch
    {
        "permits" => "Autorizații, certificate, nomenclator (data.gov.ro)",
        "osm" => "OpenStreetMap: adrese, cartiere, șantiere",
        "planning" => "PUZ/PUD și hotărâri de consiliu (primariaclujnapoca.ro)",
        "ins" => "INS TEMPO: statistici pe localități",
        "payments" => "Plățile primăriei (data.gov.ro)",
        "sicap" => "Contracte de achiziții publice (SICAP)",
        _ => source,
    };

    public static string For(ClujImo.Domain.Geo.GeoPrecision precision) => precision switch
    {
        ClujImo.Domain.Geo.GeoPrecision.Address => "adresă exactă",
        ClujImo.Domain.Geo.GeoPrecision.NearbyNumber => "număr apropiat",
        ClujImo.Domain.Geo.GeoPrecision.Street => "nivel de stradă",
        _ => "nelocalizat",
    };

    public static readonly string[] Months = ["ian", "feb", "mar", "apr", "mai", "iun", "iul", "aug", "sep", "oct", "nov", "dec"];
}
