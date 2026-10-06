using Worknexa.Domain.Enums;

namespace Worknexa.Domain.Productivity;

public static class WorkConventions
{
    public const string TaskAttachment = "Task";

    public static bool IsOpen(WorkTaskStatus status)
    {
        return status is not WorkTaskStatus.Completed and not WorkTaskStatus.Cancelled;
    }

    public static bool IsUrgent(WorkPriority priority)
    {
        return priority is WorkPriority.High or WorkPriority.Critical;
    }
}
