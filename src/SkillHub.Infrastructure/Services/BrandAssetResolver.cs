namespace SkillHub.Infrastructure.Services;

internal static class BrandAssetResolver
{
    public const string HorizontalLogoFileName = "skillhub-logo-horizontal.png";
    public const string EmblemFileName = "skillhub-emblem.png";
    public const string WatermarkLogoFileName = "logo1.png";

    public static string? ResolveAssetPath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Resources", "Assets", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Assets", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "SkillHub.Infrastructure", "Resources", "Assets", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "SkillHub.Infrastructure", "Resources", "Assets", fileName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
