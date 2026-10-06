using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Search;

namespace Worknexa.Api.Search;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapWorknexaSearch(this RouteGroupBuilder api)
    {
        api.MapGet("/search", async (string? q, string? entities, int? limit, IWorkspaceSearch search, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var types = string.IsNullOrWhiteSpace(entities)
                ? null
                : entities.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var groups = await search.SearchAsync(
                currentUser.UserId ?? throw new UnauthorizedAccessException(),
                new SearchRequest(q ?? "", types, limit ?? 5),
                cancellationToken);
            return ApiResults.Success(groups);
        }).RequireAuthorization();

        return api;
    }
}
