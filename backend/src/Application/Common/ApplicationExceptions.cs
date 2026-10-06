namespace Worknexa.Application.Common;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class InvalidSortException : Exception
{
    public InvalidSortException(string propertyName)
        : base($"Sorting by '{propertyName}' is not allowed.")
    {
        PropertyName = propertyName;
    }

    public string PropertyName { get; }
}
