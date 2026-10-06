namespace Worknexa.Application.Platform;

public static class ActivityPhrases
{
    public static string Assigned(string actor, string title, string assignee)
    {
        return $"{actor} assigned {title} to {assignee}.";
    }

    public static string Completed(string actor, string title)
    {
        return $"{actor} completed {title}.";
    }

    public static string Blocked(string actor, string title)
    {
        return $"{actor} blocked {title}.";
    }

    public static string Moved(string name, string status)
    {
        return $"{name} moved to {status}.";
    }

    public static string Created(string actor, string name)
    {
        return $"{actor} created {name}.";
    }

    public static string Scheduled(string actor, string title)
    {
        return $"{actor} scheduled {title}.";
    }

    public static string LeaveRequested(string employee, string leaveType)
    {
        return $"{employee} requested {leaveType} leave.";
    }

    public static string LeaveStatus(string employee, string status)
    {
        return $"{employee}'s leave is {status}.";
    }

    public static string Label(string status)
    {
        return status switch
        {
            "InProgress" => "In Progress",
            "NotStarted" => "Not Started",
            "OnHold" => "On Hold",
            _ => status
        };
    }
}
