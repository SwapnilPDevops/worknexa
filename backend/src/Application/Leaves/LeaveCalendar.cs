namespace Worknexa.Application.Leaves;

public static class LeaveCalendar
{
    public static int Days(DateOnly start, DateOnly end)
    {
        return end.DayNumber - start.DayNumber + 1;
    }

    public static bool Covers(DateOnly start, DateOnly end, DateOnly day)
    {
        return day >= start && day <= end;
    }

    public static bool Overlaps(DateOnly start, DateOnly end, DateOnly rangeStart, DateOnly rangeEnd)
    {
        return start <= rangeEnd && end >= rangeStart;
    }

    public static string Warning(string name, string leaveType, DateOnly start, DateOnly end)
    {
        return $"{name} is on {leaveType} leave from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}.";
    }
}
