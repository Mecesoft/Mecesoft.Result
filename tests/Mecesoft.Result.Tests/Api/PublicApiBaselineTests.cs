using Xunit;

namespace Mecesoft.Result.Tests.Api;

public sealed class PublicApiBaselineTests
{
    private static readonly string BaselinePath =
        Path.Combine(AppContext.BaseDirectory, "Api", "PublicApiBaseline.1.0.2.txt");

    [Fact]
    public void BaselineFile_Exists()
    {
        Assert.True(File.Exists(BaselinePath), $"Baseline file not found: {BaselinePath}");
    }

    [Fact]
    public void CurrentPublicApi_MatchesPublished_1_0_2_Baseline()
    {
        var expected = PublicApiDump.Normalize(File.ReadAllText(BaselinePath));
        var actual = PublicApiDump.Normalize(PublicApiDump.Dump(typeof(Result<>).Assembly));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Baseline_CoversKnownPublicSurface()
    {
        var baseline = PublicApiDump.Normalize(File.ReadAllText(BaselinePath));

        Assert.Contains("TYPE Mecesoft.Result.Result<T>", baseline, StringComparison.Ordinal);
        Assert.Contains("TYPE Mecesoft.Result.PaginationResultBase", baseline, StringComparison.Ordinal);
        Assert.Contains("TYPE Mecesoft.Result.PaginationResult<T>", baseline, StringComparison.Ordinal);
        Assert.Contains("TYPE Mecesoft.Result.PaginationMeta", baseline, StringComparison.Ordinal);
        Assert.Contains("TYPE Mecesoft.Result.PagesListQueryableExtensions", baseline, StringComparison.Ordinal);
        Assert.Contains("op_Implicit", baseline, StringComparison.Ordinal);
        Assert.Contains("ToPagedListAsync", baseline, StringComparison.Ordinal);
        Assert.Contains("[JsonIgnore]", baseline, StringComparison.Ordinal);
    }
}
