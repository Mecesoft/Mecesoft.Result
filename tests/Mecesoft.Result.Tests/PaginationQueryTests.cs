using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Mecesoft.Result.Tests;

public sealed class PaginationQueryTests
{
    [Fact]
    public async Task FirstPage_ReturnsExpectedItemsAndMetadata()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(1, 10);

        Assert.NotNull(page.Datas);
        Assert.Equal(10, page.Datas!.Count);
        Assert.Equal(Enumerable.Range(1, 10), page.Datas.Select(item => item.Id));
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.IsFirstPage);
        Assert.False(page.IsLastPage);
    }

    [Fact]
    public async Task MiddlePage_ReturnsExpectedItemsAndMetadata()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(2, 10);

        Assert.NotNull(page.Datas);
        Assert.Equal(Enumerable.Range(11, 10), page.Datas!.Select(item => item.Id));
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.IsFirstPage);
        Assert.False(page.IsLastPage);
    }

    [Fact]
    public async Task LastPage_ReturnsRemainingItemsAndMetadata()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(3, 10);

        Assert.NotNull(page.Datas);
        Assert.Equal(Enumerable.Range(21, 5), page.Datas!.Select(item => item.Id));
        Assert.Equal(3, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task PageBeyondLast_ReturnsEmptyListWithRequestedMetadata()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(4, 10);

        Assert.NotNull(page.Datas);
        Assert.Empty(page.Datas!);
        Assert.Equal(4, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task EmptyQuery_PreservesRequestedMetadataAndNullDatas()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .Where(item => item.Id > 100)
            .OrderBy(item => item.Id)
            .ToPagedListAsync(2, 10);

        Assert.Null(page.Datas);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.True(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task EmptyQuery_WithInvalidPagination_PreservesNormalizedMetadata()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .Where(item => item.Id > 100)
            .OrderBy(item => item.Id)
            .ToPagedListAsync(0, 0);

        Assert.Null(page.Datas);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.True(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task InvalidPageNumberAndSize_AreNormalized()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(0, 0);

        Assert.NotNull(page.Datas);
        Assert.Equal(10, page.Datas!.Count);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task NegativePageNumberAndSize_AreNormalized()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(-5, -1);

        Assert.NotNull(page.Datas);
        Assert.Equal(10, page.Datas!.Count);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task PageSizeLargerThanCount_ReturnsAllItems()
    {
        await using var db = CreateContext(5);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(1, 100);

        Assert.NotNull(page.Datas);
        Assert.Equal(5, page.Datas!.Count);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(100, page.PageSize);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.True(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task ReturnedEntities_AreNotTracked()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(1, 10);

        Assert.NotNull(page.Datas);
        foreach (var item in page.Datas!)
        {
            Assert.Equal(EntityState.Detached, db.Entry(item).State);
        }
    }

    [Fact]
    public async Task PreCanceledToken_ThrowsOperationCanceledException()
    {
        await using var db = CreateContext(25);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            db.Items.OrderBy(item => item.Id).ToPagedListAsync(1, 10, cancellation.Token));
    }

    [Fact]
    public async Task HugePageNumber_DoesNotOverflow_AndReturnsEmptyPage()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(300_000_000, 10);

        Assert.NotNull(page.Datas);
        Assert.Empty(page.Datas!);
        Assert.Equal(300_000_000, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task IntMaxPageNumber_WithIntMaxPageSize_DoesNotOverflow()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(int.MaxValue, int.MaxValue);

        Assert.NotNull(page.Datas);
        Assert.Empty(page.Datas!);
        Assert.Equal(int.MaxValue, page.PageNumber);
        Assert.Equal(int.MaxValue, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.False(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task OffsetExactlyAtCount_ReturnsEmptyPage()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .OrderBy(item => item.Id)
            .ToPagedListAsync(6, 5);

        Assert.NotNull(page.Datas);
        Assert.Empty(page.Datas!);
        Assert.Equal(6, page.PageNumber);
        Assert.Equal(5, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(5, page.TotalPages);
        Assert.False(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    [Fact]
    public async Task EmptyQuery_WithHugePageValues_PreservesMetadataAndNullDatas()
    {
        await using var db = CreateContext(25);

        var page = await db.Items
            .Where(item => item.Id > 100)
            .OrderBy(item => item.Id)
            .ToPagedListAsync(int.MaxValue, int.MaxValue);

        Assert.Null(page.Datas);
        Assert.Equal(int.MaxValue, page.PageNumber);
        Assert.Equal(int.MaxValue, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.True(page.IsFirstPage);
        Assert.True(page.IsLastPage);
    }

    private static TestDbContext CreateContext(int itemCount)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var db = new TestDbContext(options);
        if (itemCount > 0)
        {
            db.Items.AddRange(Enumerable.Range(1, itemCount)
                .Select(id => new TestItem { Id = id, Name = $"Item {id}" }));
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        return db;
    }

    private sealed class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestItem> Items => Set<TestItem>();
    }
}
