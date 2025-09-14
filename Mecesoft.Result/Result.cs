using System.Net;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Mecesoft.Result;

/// <summary>
/// Generic result wrapper type that optionally carries pagination metadata.
/// </summary>
/// <typeparam name="T">Payload type</typeparam>
public sealed class Result<T>
{
    public T? Data { get; set; }
    public object? Pagination { get; set; }
    public List<string>? ErrorMessages { get; set; }
    public bool IsSuccessful { get; set; } = true;

    [JsonIgnore]
    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    public Result(T data)
    {
        Data = data;
    }

    public Result(T data, PaginationResultBase pagination)
    {
        Data = data;
        Pagination = pagination;
    }

    public Result(T data, object pagination)
    {
        Data = data;
        Pagination = pagination;
    }

    public Result(int statusCode, List<string> errorMessages)
    {
        IsSuccessful = false;
        StatusCode = statusCode;
        ErrorMessages = errorMessages;
    }

    public Result(int statusCode, string errorMessage)
    {
        IsSuccessful = false;
        StatusCode = statusCode;
        ErrorMessages = new() { errorMessage };
    }

    public static implicit operator Result<T>(T data)
    {
        return new(data);
    }

    public static implicit operator Result<T>((int statusCode, List<string> errorMessages) parameters)
    {
        return new(parameters.statusCode, parameters.errorMessages);
    }

    public static implicit operator Result<T>((int statusCode, string errorMessage) parameters)
    {
        return new(parameters.statusCode, parameters.errorMessage);
    }

    public static Result<T> Succeed(T data)
    {
        return new(data);
    }

    public static Result<T> Succeed(T data, PaginationResultBase pagination)
    {
        return new(data, pagination);
    }

    public static Result<T> Succeed(T data, object pagination)
    {
        return new(data, pagination);
    }

    public static Result<T> Failure(int statusCode, List<string> errorMessages)
    {
        return new(statusCode, errorMessages);
    }

    public static Result<T> Failure(int statusCode, string errorMessage)
    {
        return new(statusCode, errorMessage);
    }

    public static Result<T> Failure(string errorMessage)
    {
        return new((int)HttpStatusCode.InternalServerError, errorMessage);
    }

    public static Result<T> Failure(List<string> errorMessages)
    {
        return new((int)HttpStatusCode.InternalServerError, errorMessages);
    }
}

public class PaginationResultBase
{
    public PaginationResultBase(int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        TotalCount = totalCount;
        IsFirstPage = PageNumber <= 1 || TotalPages == 0;
        IsLastPage = TotalPages == 0 || PageNumber >= TotalPages;
    }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool IsFirstPage { get; set; }
    public bool IsLastPage { get; set; }
}

public class PaginationResult<T> : PaginationResultBase
{
    public PaginationResult(IList<T>? datas, int pageNumber, int pageSize, int totalCount)
        : base(pageNumber, pageSize, totalCount)
    {
        Datas = datas;
    }

    public IList<T>? Datas { get; set; }
}

public static class PagesListQueryableExtensions
{
    /// <summary>
    /// Paginates an IQueryable using EF Core async operations and returns items with pagination metadata.
    /// </summary>
    public static async Task<PaginationResult<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var count = await source.CountAsync(cancellationToken);
        if (count > 0)
        {
            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new(items, pageNumber, pageSize, count);
        }

        return new(null, 0, 0, 0);
    }
}
