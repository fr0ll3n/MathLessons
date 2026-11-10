using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

public interface ISubscriberRankService
{
    RankInfo GetRank(User user);
}

public sealed class RankInfo
{
    public string Name { get; init; } = "None";
    public string BadgeClass { get; init; } = "secondary";
    public int MonthsSubscribed { get; init; }
    public int? MonthsUntilNext { get; init; }
    public string? NextRankName { get; init; }
    public bool HasBadge => Name != "None";
}
