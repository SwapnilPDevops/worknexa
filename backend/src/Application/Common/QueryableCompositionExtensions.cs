using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Worknexa.Application.Common;

public static class QueryableCompositionExtensions
{
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        SortRequest sort,
        IReadOnlySet<string> allowedProperties)
    {
        if (string.IsNullOrWhiteSpace(sort.SortBy))
        {
            return query;
        }

        if (!allowedProperties.Contains(sort.SortBy))
        {
            throw new InvalidSortException(sort.SortBy);
        }

        var property = typeof(T).GetProperty(sort.SortBy);
        if (property is null)
        {
            throw new InvalidSortException(sort.SortBy);
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        var body = Expression.Property(parameter, property);
        var keySelector = Expression.Lambda(body, parameter);
        var methodName = sort.Direction == SortDirection.Desc ? "OrderByDescending" : "OrderBy";
        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                candidate.Name == methodName
                && candidate.IsGenericMethodDefinition
                && candidate.GetParameters().Length == 2);

        var call = Expression.Call(
            method.MakeGenericMethod(typeof(T), property.PropertyType),
            query.Expression,
            Expression.Quote(keySelector));

        return query.Provider.CreateQuery<T>(call);
    }

    public static IQueryable<T> AsReadQuery<T>(this IQueryable<T> query)
        where T : class
    {
        return query.AsNoTracking();
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
