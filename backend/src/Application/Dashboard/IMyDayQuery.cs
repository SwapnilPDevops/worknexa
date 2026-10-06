namespace Worknexa.Application.Dashboard;

public interface IMyDayQuery
{
    Task<MyDaySnapshot> LoadAsync(Guid userId, MyDayWindow window, MyDayScope scope, CancellationToken cancellationToken);
}
