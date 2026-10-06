namespace Worknexa.Application.Configuration;

public sealed class PaginationSettings
{
    public const string SectionName = "Pagination";

    public int DefaultPageSize { get; init; } = 20;

    public int MaxPageSize { get; init; } = 100;
}
