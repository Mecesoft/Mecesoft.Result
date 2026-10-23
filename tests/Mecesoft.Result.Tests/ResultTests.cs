using Xunit;

namespace Mecesoft.Result.Tests;

public sealed class ResultTests
{
    [Fact]
    public void DefaultConstructorState_IsSuccessfulWithOkStatus()
    {
        var result = new Result<string>("payload");

        Assert.Equal("payload", result.Data);
        Assert.Null(result.Pagination);
        Assert.Null(result.ErrorMessages);
        Assert.True(result.IsSuccessful);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void Succeed_SetsPayloadAndSuccessState()
    {
        var result = Result<string>.Succeed("payload");

        Assert.Equal("payload", result.Data);
        Assert.True(result.IsSuccessful);
        Assert.Equal(200, result.StatusCode);
        Assert.Null(result.ErrorMessages);
        Assert.Null(result.Pagination);
    }

    [Fact]
    public void Succeed_NullReferencePayload_IsStillSuccessful()
    {
        var result = Result<string>.Succeed(null!);

        Assert.Null(result.Data);
        Assert.True(result.IsSuccessful);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void Failure_String_SetsFailureState()
    {
        var result = Result<string>.Failure(400, "bad request");

        Assert.Null(result.Data);
        Assert.False(result.IsSuccessful);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ErrorMessages);
        Assert.Equal(new[] { "bad request" }, result.ErrorMessages);
    }

    [Fact]
    public void Failure_List_SetsFailureState()
    {
        var result = Result<string>.Failure(422, new List<string> { "a", "b" });

        Assert.Null(result.Data);
        Assert.False(result.IsSuccessful);
        Assert.Equal(422, result.StatusCode);
        Assert.Equal(new[] { "a", "b" }, result.ErrorMessages);
    }

    [Fact]
    public void Failure_String_DefaultStatusCodeIs500()
    {
        var result = Result<string>.Failure("unexpected");

        Assert.False(result.IsSuccessful);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public void Failure_List_DefaultStatusCodeIs500()
    {
        var result = Result<string>.Failure(new List<string> { "unexpected" });

        Assert.False(result.IsSuccessful);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public void ImplicitConversion_FromPayload_CreatesSuccessfulResult()
    {
        Result<string> result = "payload";

        Assert.Equal("payload", result.Data);
        Assert.True(result.IsSuccessful);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void ImplicitConversion_FromNullPayload_CreatesSuccessfulResult()
    {
        Result<string> result = (string)null!;

        Assert.NotNull(result);
        Assert.Null(result.Data);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ImplicitConversion_FromStringTuple_CreatesFailure()
    {
        Result<string> result = (400, "bad request");

        Assert.False(result.IsSuccessful);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(new[] { "bad request" }, result.ErrorMessages);
    }

    [Fact]
    public void ImplicitConversion_FromListTuple_CreatesFailure()
    {
        Result<string> result = (422, new List<string> { "a", "b" });

        Assert.False(result.IsSuccessful);
        Assert.Equal(422, result.StatusCode);
        Assert.Equal(new[] { "a", "b" }, result.ErrorMessages);
    }

    [Fact]
    public void Succeed_WithPaginationResultBase_SetsPagination()
    {
        var pagination = new PaginationResultBase(2, 10, 25);
        var result = Result<string>.Succeed("payload", pagination);

        Assert.Same(pagination, result.Pagination);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Succeed_WithObjectPagination_SetsPagination()
    {
        var pagination = new PaginationMeta
        {
            PageNumber = 2,
            PageSize = 10,
            TotalRecords = 25,
            TotalPages = 3,
            HasNextPage = true,
            HasPreviousPage = true
        };

        var result = Result<string>.Succeed("payload", pagination);

        Assert.Same(pagination, result.Pagination);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Failure_WithNullString_StoresNullEntry()
    {
        var result = Result<string>.Failure((string)null!);

        Assert.False(result.IsSuccessful);
        Assert.Equal(500, result.StatusCode);
        Assert.NotNull(result.ErrorMessages);
        Assert.Single(result.ErrorMessages!);
        Assert.Null(result.ErrorMessages![0]);
    }

    [Fact]
    public void Failure_WithNullList_LeavesErrorMessagesNull()
    {
        var result = Result<string>.Failure((List<string>)null!);

        Assert.False(result.IsSuccessful);
        Assert.Equal(500, result.StatusCode);
        Assert.Null(result.ErrorMessages);
    }

    [Fact]
    public void Failure_ListIsStoredByReference()
    {
        var messages = new List<string> { "first" };
        var result = Result<string>.Failure(400, messages);

        messages.Add("second");

        Assert.Equal(new[] { "first", "second" }, result.ErrorMessages);
    }

    [Fact]
    public void Failure_ValueTypePayload_IsDefaultValue()
    {
        var result = Result<int>.Failure(500, "failure");

        Assert.Equal(0, result.Data);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void StatusCode_IsNotValidated()
    {
        var result = Result<string>.Failure(0, "invalid status");

        Assert.Equal(0, result.StatusCode);
        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void PropertySetters_AllowInconsistentState()
    {
        var result = Result<string>.Succeed("payload");

        result.IsSuccessful = false;
        result.StatusCode = 500;

        Assert.False(result.IsSuccessful);
        Assert.Equal(500, result.StatusCode);
        Assert.Null(result.ErrorMessages);
    }

    [Fact]
    public void Constructor_Failure_WithList_SetsState()
    {
        var result = new Result<string>(409, new List<string> { "conflict" });

        Assert.False(result.IsSuccessful);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(new[] { "conflict" }, result.ErrorMessages);
    }

    [Fact]
    public void Constructor_Failure_WithString_SetsState()
    {
        var result = new Result<string>(409, "conflict");

        Assert.False(result.IsSuccessful);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(new[] { "conflict" }, result.ErrorMessages);
    }
}
