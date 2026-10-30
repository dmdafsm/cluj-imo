using System.Text.RegularExpressions;
using ClujImo.Domain.Text;

namespace ClujImo.Domain.Classification;

/// <summary>
/// Deterministic, rule-based classification of the free-text work description ("lucrari" / "scop").
/// Rules are ordered: first match wins. Every rule is covered by a test with a real description.
/// </summary>
public static partial class WorkClassifier
{
    private static readonly (WorkCategory Category, Regex Pattern)[] BuildingTypes =
    [
        (WorkCategory.NewSemiCollectiveResidential, Rx(@"SEMI\s*-?\s*COLECTIV|DUPLEX|LOCUINT\w* CUPLAT|DOUA UNITATI (LOCATIVE|DE LOCUIT)")),
        (WorkCategory.NewMixedUse, Rx(@"\bMIXT|FUNCTIUN\w* MIXT")),
        (WorkCategory.NewCollectiveResidential, Rx(@"LOCUINT\w* COLECTIV|IMOBIL\w* (DE|CU) (LOCUINT|APARTAMENT)|BLOC\w* (DE )?LOCUINT|ANSAMBLU REZIDENTIAL|\bREZIDENTIAL\b|\b([3-9]|\d{2,}) APARTAMENTE")),
        (WorkCategory.NewSingleFamily, Rx(@"UNIFAMILIAL|CASA FAMILIALA|LOCUINT\w* FAMILIAL|LOCUINTA INDIVIDUALA|CASA DE LOCUIT|\bLOCUINTA\b|\bCASA\b")),
        (WorkCategory.NewOffice, Rx(@"\bBIROU|\bSEDIU")),
        (WorkCategory.NewPublic, Rx(@"SCOAL|GRADINIT|\bCRESA|LICEU|SPITAL|CLINIC|BISERIC|CAPELA|MANASTIR|CAMPUS|CAMIN|UNIVERSIT|SALA DE SPORT|BAZIN|STADION|CENTRU (SOCIAL|CULTURAL|COMUNITAR|DE ZI)")),
        (WorkCategory.NewIndustrial, Rx(@"\bHAL[AE]\b|DEPOZIT|PRODUCTIE|LOGISTIC|ATELIER|INDUSTRIAL")),
        (WorkCategory.NewCommercial, Rx(@"COMERT|COMERCIAL|MAGAZIN|SUPERMARKET|HOTEL|PENSIUNE|RESTAURANT|SHOWROOM|SERVICII|BENZINARIE|CARBURANT|SPALATORIE")),
    ];

    private static readonly (WorkCategory Category, Regex Pattern)[] OtherWorks =
    [
        (WorkCategory.Extension, Rx(@"EXTINDERE|MANSARD|SUPRAETAJ|ETAJARE|ADAUGIRE")),
        (WorkCategory.Demolition, Rx(@"DESFIINTARE|DEMOLARE")),
        (WorkCategory.Infrastructure, Rx(@"FOTOVOLTAIC|PANOURI SOLARE|BRANSAMENT|RACORD|\bRETE(A|LE)\b|CONDUCT|CANALIZARE|ALIMENTARE CU|POST (DE )?TRANSFORM|\bLE[SA]\b|FIBRA|TELECOMUNICATII|ANTEN|STATIE DE BAZA|\bDRUM|\bSTRAZ|PARCAJ|PARCARE|TROTUAR|PISTA|\bALEE|\bPOD\b|ILUMINAT|INCARCARE|GAZE NATURALE")),
        (WorkCategory.Signage, Rx(@"\bFIRM[AE]|RECLAM|PUBLICITAR|\bPANOU|TOTEM|AFISAJ|INSCRIPTIONAR")),
        (WorkCategory.Renovation, Rx(@"MODIFICAR\w* INTERIOAR|RECOMPARTIMENTAR|REABILITARE|RENOVARE|CONSOLIDARE|RESTAURARE|ANVELOPARE|TERMOIZOL|SCHIMBARE (DE )?(DESTINATIE|FUNCTIUNE)|INCHIDERE BALCON|REFACERE|MODERNIZARE|AMENAJAR\w* INTERIOAR|ACCES SEPARAT|DESCHIDERE|GOL DE|FATAD|INVELITOARE|ACOPERIS|LIFT|AMENAJARE SPATIU|REPARATI|REMODELARE|COMPARTIMENTAR|EFICIENT\w* ENERGETIC|TERMOSISTEM|TAMPLARIE")),
        (WorkCategory.SiteWorks, Rx(@"IMPREJMUIR|AMENAJAR\w* EXTERIOAR|ORGANIZARE DE SANTIER|PISCIN|FOISOR|ZID DE SPRIJIN|SPATII VERZI|LOC DE JOACA|TERAS|COPERTIN|PERGOL|GARAJ|CARPORT|ANEX")),
        (WorkCategory.Administrative, Rx(@"INFORMARE|VANZARE|CUMPARARE|DEZMEMBRARE|ALIPIRE|DEZLIPIRE|PARCELARE|INTABULARE|CONCESIONARE|NOTARIAL|DIVIZARE|LITIGI|SUCCESIUNE|INCHIRIERE|OBTINERE (AVIZ|ACORD)|ATESTARE|CERTIFICAT")),
    ];

    private static Regex RenovationWorks => OtherWorks.First(w => w.Category == WorkCategory.Renovation).Pattern;

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.Ordinal)
    {
        ["DOUA"] = 2, ["DOI"] = 2, ["TREI"] = 3, ["PATRU"] = 4, ["CINCI"] = 5, ["SASE"] = 6, ["SAPTE"] = 7, ["OPT"] = 8,
    };

    public static WorkClassification Classify(string? description)
    {
        var text = TextNormalizer.ForMatching(description);

        var isContinuation = text.Contains("CONTINUARE", StringComparison.Ordinal);
        var isAmendment = Amendment().IsMatch(text);
        var includesDemolition = Demolition().IsMatch(text);
        var isUrbanPlan = UrbanPlan().IsMatch(text);
        var (regime, levels) = ExtractHeightRegime(text);
        var units = ExtractUnits(text);

        return new WorkClassification(
            DetermineCategory(text, units, refersToEarlierPermit: isContinuation || isAmendment),
            isContinuation,
            isAmendment,
            includesDemolition,
            isUrbanPlan,
            regime,
            levels,
            units);
    }

    private static WorkCategory DetermineCategory(string text, int? units, bool refersToEarlierPermit)
    {
        if (text.Length == 0)
        {
            return WorkCategory.Other;
        }

        // A continuation/amendment names the original building without a verb ("... LA CASA UNIFAMILIALA S+P+E").
        var builds = BuildVerb().IsMatch(text)
            || (refersToEarlierPermit && !OtherWorks[0].Pattern.IsMatch(text) && !RenovationWorks.IsMatch(text));
        if (builds)
        {
            foreach (var (category, pattern) in BuildingTypes)
            {
                if (pattern.IsMatch(text))
                {
                    // Single-family wording but many units means a collective building.
                    return category == WorkCategory.NewSingleFamily && units >= 3
                        ? WorkCategory.NewCollectiveResidential
                        : category;
                }
            }

            if (BuildingNoun().IsMatch(text))
            {
                return WorkCategory.NewOtherBuilding;
            }
        }

        foreach (var (category, pattern) in OtherWorks)
        {
            if (pattern.IsMatch(text))
            {
                return category;
            }
        }

        return WorkCategory.Other;
    }

    /// <summary>Finds height regimes like "2S+P+10E+2R" or "D + P + E + M" and returns the tallest.</summary>
    internal static (string? Regime, int? Levels) ExtractHeightRegime(string text)
    {
        var compact = PlusSpacing().Replace(text, "+");
        string? bestRegime = null;
        int? bestLevels = null;

        foreach (Match match in RegimeCandidate().Matches(compact))
        {
            var tokens = match.Value.Split('+');
            var groundIndex = Array.IndexOf(tokens, "P");
            if (groundIndex < 0 || !tokens[..groundIndex].All(t => BelowGround().IsMatch(t)))
            {
                continue;
            }

            var levels = 1;
            var valid = true;
            foreach (var token in tokens[(groundIndex + 1)..])
            {
                var level = AboveGround().Match(token);
                if (!level.Success)
                {
                    valid = false;
                    break;
                }

                levels += level.Groups[1].Length > 0 ? int.Parse(level.Groups[1].ValueSpan, provider: null) : 1;
            }

            if (valid && levels > (bestLevels ?? 0))
            {
                bestLevels = levels;
                bestRegime = match.Value;
            }
        }

        return (bestRegime, bestLevels);
    }

    internal static int? ExtractUnits(string text)
    {
        int? best = null;
        foreach (Match match in UnitCount().Matches(text))
        {
            var raw = match.Groups[1].Value;
            var count = NumberWords.TryGetValue(raw, out var word) ? word : int.Parse(raw, provider: null);
            if (count is > 0 and < 5000 && count > (best ?? 0))
            {
                best = count;
            }
        }

        return best;
    }

    private static Regex Rx(string pattern) => new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Besides explicit verbs: descriptions that start with the building ("LOCUINTA SEMICOLECTIVA P+E ...")
    // and continuations that name it ("... EMISA PENTRU LOCUINTA FAMILIALA").
    [GeneratedRegex(@"^(LOCUINT|IMOBIL|CASA|CLADIR|ANSAMBLU|BLOC|HALA)|EMIS\w* PENTRU\W+(UN |O )?(LOCUINT|IMOBIL|CASA|CLADIR|ANSAMBLU|BLOC|HALA)|\bCONSTRUIRE|\bEDIFICARE|\bCONSTRUCTIE NOUA|REALIZARE (IMOBIL|CLADIR|LOCUINT|CONSTRUCTI|ANSAMBLU|HAL|CORP)|EXECUTARE (IMOBIL|CLADIR|LOCUINT)|RIDICARE (IMOBIL|CLADIR|CONSTRUCTI)")]
    private static partial Regex BuildVerb();

    [GeneratedRegex(@"IMOBIL|CLADIR|CONSTRUCTI|CORP")]
    private static partial Regex BuildingNoun();

    [GeneratedRegex(@"MODIFICAR\w* (DE TEMA|PE PARCURSUL|(A )?SOLUTIE)|MODIFICARE (A\.?C|AUTORIZATI)")]
    private static partial Regex Amendment();

    [GeneratedRegex(@"DESFIINTARE|DEMOLARE")]
    private static partial Regex Demolition();

    [GeneratedRegex(@"\bP\.?\s?U\.?\s?[ZD]\b|PLAN URBANISTIC")]
    private static partial Regex UrbanPlan();

    [GeneratedRegex(@"\s*\+\s*")]
    private static partial Regex PlusSpacing();

    [GeneratedRegex(@"(?<![A-Z0-9])[0-9A-Z]{1,8}(?:\+[0-9A-Z]{1,8})+(?![A-Z0-9])")]
    private static partial Regex RegimeCandidate();

    [GeneratedRegex(@"^\d?(S|D|DS|SP|SD)$")]
    private static partial Regex BelowGround();

    [GeneratedRegex(@"^(\d{0,2})(E|ER|ETR|ETH|R|M|ETAJ|ETAJE|MANSARDA)$")]
    private static partial Regex AboveGround();

    [GeneratedRegex(@"\b(\d{1,4}|DOUA|DOI|TREI|PATRU|CINCI|SASE|SAPTE|OPT)\s+(?:DE\s+)?(?:APARTAMENTE|UNITATI LOCATIVE|UNITATI DE LOCUIT|LOCUINTE|UNITATI INDIVIDUALE|AP\b)")]
    private static partial Regex UnitCount();
}
