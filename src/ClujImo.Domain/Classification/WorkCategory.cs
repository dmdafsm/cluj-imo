namespace ClujImo.Domain.Classification;

public enum WorkCategory
{
    Other = 0,
    NewCollectiveResidential = 1,
    NewSemiCollectiveResidential = 2,
    NewSingleFamily = 3,
    NewMixedUse = 4,
    NewOffice = 5,
    NewCommercial = 6,
    NewIndustrial = 7,
    NewPublic = 8,
    NewOtherBuilding = 9,
    Extension = 20,
    Renovation = 21,
    Demolition = 22,
    Infrastructure = 23,
    Signage = 24,
    SiteWorks = 25,
    Administrative = 26,
}

public static class WorkCategoryExtensions
{
    public static bool IsNewBuild(this WorkCategory category) =>
        category is >= WorkCategory.NewCollectiveResidential and <= WorkCategory.NewOtherBuilding;

    public static bool IsResidential(this WorkCategory category) =>
        category is WorkCategory.NewCollectiveResidential or WorkCategory.NewSemiCollectiveResidential
            or WorkCategory.NewSingleFamily or WorkCategory.NewMixedUse;
}
