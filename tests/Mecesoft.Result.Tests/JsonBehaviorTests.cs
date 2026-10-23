using System.Text.Json;
using Xunit;

namespace Mecesoft.Result.Tests;

public sealed class JsonBehaviorTests
{
    [Fact]
    public void Serialize_Success_UsesDefaultPropertyNamesAndOmitsStatusCode()
    {
        var result = Result<string>.Succeed("payload");

        var json = JsonSerializer.Serialize(result);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("payload", root.GetProperty("Data").GetString());
        Assert.True(root.GetProperty("IsSuccessful").GetBoolean());
        Assert.False(root.TryGetProperty("StatusCode", out _));
        Assert.True(root.TryGetProperty("Pagination", out var pagination));
        Assert.Equal(JsonValueKind.Null, pagination.ValueKind);
        Assert.True(root.TryGetProperty("ErrorMessages", out var errors));
        Assert.Equal(JsonValueKind.Null, errors.ValueKind);
    }

    [Fact]
    public void Serialize_Failure_OmitsStatusCodeAndIncludesMessages()
    {
        var result = Result<string>.Failure(404, "not found");

        var json = JsonSerializer.Serialize(result);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.False(root.GetProperty("IsSuccessful").GetBoolean());
        Assert.False(root.TryGetProperty("StatusCode", out _));
        Assert.Equal(JsonValueKind.Array, root.GetProperty("ErrorMessages").ValueKind);
        Assert.Equal("not found", root.GetProperty("ErrorMessages")[0].GetString());
    }

    [Fact]
    public void Serialize_PaginationResultBase_UsesRuntimeType()
    {
        var result = Result<string>.Succeed("payload", new PaginationResultBase(2, 10, 25));

        var json = JsonSerializer.Serialize(result);
        using var document = JsonDocument.Parse(json);
        var pagination = document.RootElement.GetProperty("Pagination");

        Assert.Equal(2, pagination.GetProperty("PageNumber").GetInt32());
        Assert.Equal(10, pagination.GetProperty("PageSize").GetInt32());
        Assert.Equal(3, pagination.GetProperty("TotalPages").GetInt32());
        Assert.Equal(25, pagination.GetProperty("TotalCount").GetInt32());
        Assert.False(pagination.GetProperty("IsFirstPage").GetBoolean());
        Assert.False(pagination.GetProperty("IsLastPage").GetBoolean());
    }

    [Fact]
    public void Serialize_PaginationMeta_UsesRuntimeType()
    {
        var meta = PaginationMeta.Create(2, 10, 25, 3);
        var result = Result<string>.Succeed("payload", meta);

        var json = JsonSerializer.Serialize(result);
        using var document = JsonDocument.Parse(json);
        var pagination = document.RootElement.GetProperty("Pagination");

        Assert.Equal(2, pagination.GetProperty("PageNumber").GetInt32());
        Assert.Equal(10, pagination.GetProperty("PageSize").GetInt32());
        Assert.Equal(25, pagination.GetProperty("TotalRecords").GetInt64());
        Assert.Equal(3, pagination.GetProperty("TotalPages").GetInt32());
        Assert.True(pagination.GetProperty("HasNextPage").GetBoolean());
        Assert.True(pagination.GetProperty("HasPreviousPage").GetBoolean());
    }

    [Fact]
    public void Serialize_PaginationResult_IncludesDatas()
    {
        var paged = new PaginationResult<string>(new List<string> { "a", "b" }, 1, 2, 2);
        var result = Result<PaginationResult<string>>.Succeed(paged);

        var json = JsonSerializer.Serialize(result);
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement.GetProperty("Data");

        Assert.Equal(2, data.GetProperty("Datas").GetArrayLength());
        Assert.Equal("a", data.GetProperty("Datas")[0].GetString());
        Assert.Equal(2, data.GetProperty("TotalCount").GetInt32());
    }

    [Fact]
    public void Deserialize_Result_IsNotSupported()
    {
        var json = JsonSerializer.Serialize(Result<string>.Succeed("payload"));

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Result<string>>(json));
    }

    [Fact]
    public void PaginationResultBase_RoundTrips_UsingItsSingleConstructor()
    {
        var original = new PaginationResultBase(2, 10, 25);
        var json = JsonSerializer.Serialize(original);

        var roundTripped = JsonSerializer.Deserialize<PaginationResultBase>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(2, roundTripped!.PageNumber);
        Assert.Equal(10, roundTripped.PageSize);
        Assert.Equal(25, roundTripped.TotalCount);
        Assert.Equal(3, roundTripped.TotalPages);
        Assert.False(roundTripped.IsFirstPage);
        Assert.False(roundTripped.IsLastPage);
    }

    [Fact]
    public void PaginationResult_RoundTrips_UsingItsSingleConstructor()
    {
        var original = new PaginationResult<string>(new List<string> { "a", "b" }, 2, 10, 25);
        var json = JsonSerializer.Serialize(original);

        var roundTripped = JsonSerializer.Deserialize<PaginationResult<string>>(json);

        Assert.NotNull(roundTripped);
        Assert.NotNull(roundTripped!.Datas);
        Assert.Equal(new[] { "a", "b" }, roundTripped.Datas);
        Assert.Equal(2, roundTripped.PageNumber);
        Assert.Equal(10, roundTripped.PageSize);
        Assert.Equal(25, roundTripped.TotalCount);
        Assert.Equal(3, roundTripped.TotalPages);
        Assert.False(roundTripped.IsFirstPage);
        Assert.False(roundTripped.IsLastPage);
    }

    [Fact]
    public void PaginationMeta_RoundTrips()
    {
        var original = PaginationMeta.Create(2, 10, 25, 3);

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<PaginationMeta>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.PageNumber, roundTripped!.PageNumber);
        Assert.Equal(original.PageSize, roundTripped.PageSize);
        Assert.Equal(original.TotalRecords, roundTripped.TotalRecords);
        Assert.Equal(original.TotalPages, roundTripped.TotalPages);
        Assert.Equal(original.HasNextPage, roundTripped.HasNextPage);
        Assert.Equal(original.HasPreviousPage, roundTripped.HasPreviousPage);
    }

    [Fact]
    public void Serialize_ValueTypeFailure_DataIsZero()
    {
        var result = Result<int>.Failure(500, "failure");

        var json = JsonSerializer.Serialize(result);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(0, document.RootElement.GetProperty("Data").GetInt32());
        Assert.False(document.RootElement.GetProperty("IsSuccessful").GetBoolean());
    }
}
