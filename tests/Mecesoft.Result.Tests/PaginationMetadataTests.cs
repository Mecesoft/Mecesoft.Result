using Xunit;

namespace Mecesoft.Result.Tests;

public sealed class PaginationMetadataTests
{
    [Fact]
    public void PaginationResultBase_ComputesTotalPages_WhenTotalIsDivisible()
    {
        var pagination = new PaginationResultBase(1, 10, 100);

        Assert.Equal(1, pagination.PageNumber);
        Assert.Equal(10, pagination.PageSize);
        Assert.Equal(100, pagination.TotalCount);
        Assert.Equal(10, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.False(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_RoundsTotalPagesUp_WhenTotalIsNotDivisible()
    {
        var pagination = new PaginationResultBase(1, 10, 101);

        Assert.Equal(11, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.False(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_ZeroTotal_IsFirstAndLast()
    {
        var pagination = new PaginationResultBase(1, 10, 0);

        Assert.Equal(0, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_ZeroPageSize_ProducesZeroTotalPages()
    {
        var pagination = new PaginationResultBase(1, 0, 100);

        Assert.Equal(0, pagination.PageSize);
        Assert.Equal(0, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_NegativePageSize_IsAccepted()
    {
        var pagination = new PaginationResultBase(1, -5, 100);

        Assert.Equal(-5, pagination.PageSize);
        Assert.Equal(0, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_NegativeTotalCount_CanProduceNegativeTotalPages()
    {
        var pagination = new PaginationResultBase(1, 10, -11);

        Assert.Equal(-11, pagination.TotalCount);
        Assert.Equal(-1, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_ZeroPageNumber_IsFirstPage()
    {
        var pagination = new PaginationResultBase(0, 10, 100);

        Assert.True(pagination.IsFirstPage);
        Assert.False(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_PageBeyondLast_IsLastPage()
    {
        var pagination = new PaginationResultBase(11, 10, 100);

        Assert.Equal(10, pagination.TotalPages);
        Assert.False(pagination.IsFirstPage);
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResultBase_ComputedPropertiesCanBecomeStale()
    {
        var pagination = new PaginationResultBase(1, 10, 100);

        pagination.TotalCount = 0;

        Assert.Equal(0, pagination.TotalCount);
        Assert.Equal(10, pagination.TotalPages);
        Assert.False(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResult_StoresDatasAndBaseValues()
    {
        var items = new List<string> { "a", "b" };

        var pagination = new PaginationResult<string>(items, 2, 10, 25);

        Assert.Same(items, pagination.Datas);
        Assert.Equal(2, pagination.PageNumber);
        Assert.Equal(10, pagination.PageSize);
        Assert.Equal(25, pagination.TotalCount);
        Assert.Equal(3, pagination.TotalPages);
        Assert.False(pagination.IsFirstPage);
        Assert.False(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationResult_AllowsNullDatas()
    {
        var pagination = new PaginationResult<string>(null, 1, 10, 0);

        Assert.Null(pagination.Datas);
        Assert.Equal(0, pagination.TotalPages);
        Assert.True(pagination.IsFirstPage);
        Assert.True(pagination.IsLastPage);
    }

    [Fact]
    public void PaginationMeta_Create_SetsFlagsForMiddlePage()
    {
        var meta = PaginationMeta.Create(2, 10, 25, 3);

        Assert.Equal(2, meta.PageNumber);
        Assert.Equal(10, meta.PageSize);
        Assert.Equal(25, meta.TotalRecords);
        Assert.Equal(3, meta.TotalPages);
        Assert.True(meta.HasNextPage);
        Assert.True(meta.HasPreviousPage);
    }

    [Fact]
    public void PaginationMeta_Create_SetsFlagsForLastPage()
    {
        var meta = PaginationMeta.Create(3, 10, 25, 3);

        Assert.False(meta.HasNextPage);
        Assert.True(meta.HasPreviousPage);
    }

    [Fact]
    public void PaginationMeta_Create_EmptyDataset_FirstPage()
    {
        var meta = PaginationMeta.Create(1, 10, 0, 0);

        Assert.False(meta.HasNextPage);
        Assert.False(meta.HasPreviousPage);
    }

    [Fact]
    public void PaginationMeta_Create_EmptyDataset_PageTwo_HasNoPreviousPage()
    {
        var meta = PaginationMeta.Create(2, 10, 0, 0);

        Assert.False(meta.HasNextPage);
        Assert.False(meta.HasPreviousPage);
    }

    [Fact]
    public void PaginationMeta_Create_NonEmptyDataset_PageTwo_HasPreviousPage()
    {
        var meta = PaginationMeta.Create(2, 10, 25, 3);

        Assert.True(meta.HasNextPage);
        Assert.True(meta.HasPreviousPage);
    }

    [Fact]
    public void PaginationMeta_Create_TrustsCallerSuppliedTotalPages()
    {
        var meta = PaginationMeta.Create(1, 10, 25, 0);

        Assert.Equal(25, meta.TotalRecords);
        Assert.Equal(0, meta.TotalPages);
        Assert.False(meta.HasNextPage);
    }

    [Fact]
    public void PaginationMeta_TotalRecords_RetainsLongValues()
    {
        var meta = PaginationMeta.Create(1, 10, int.MaxValue + 1L, 1);

        Assert.Equal(int.MaxValue + 1L, meta.TotalRecords);
    }
}
