namespace Worknexa.Application.Common;

public enum SortDirection
{
    Asc,
    Desc
}

public sealed class SortRequest
{
    public string? SortBy { get; set; }

    public SortDirection Direction { get; set; } = SortDirection.Asc;
}
