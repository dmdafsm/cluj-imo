namespace ClujImo.Domain.Permits;

public enum PermitKind
{
    /// <summary>Autorizație de construire (AC).</summary>
    BuildingPermit = 1,

    /// <summary>Certificat de urbanism (CU) — the intent, issued before an AC.</summary>
    UrbanismCertificate = 2,

    /// <summary>Certificat de atestare a edificării — the building is finished.</summary>
    CompletionCertificate = 3,
}
