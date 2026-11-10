using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;

namespace MathematicsLessons.Services;

/// <summary>Loyalty ranks by months subscribed: 3 Bronze … 24 Master.</summary>
public class SubscriberRankService : ISubscriberRankService
{
    private static readonly (int Months, string Name, string Css)[] Ladder =
    {
        (24, "Master", "dark"),
        (15, "Diamond", "info"),
        (12, "Emerald", "success"),
        (8, "Gold", "warning"),
        (5, "Silver", "secondary"),
        (3, "Bronze", "danger"),
    };

    public RankInfo GetRank(User user)
    {
        if (user.SubId == null || user.SubStartedAt == null)
            return new RankInfo { Name = "None", MonthsUntilNext = 3, NextRankName = "Bronze" };

        var months = FullMonths(user.SubStartedAt.Value, DateTime.UtcNow);
        string name = "None", css = "secondary";
        foreach (var (need, n, c) in Ladder)
        {
            if (months >= need) { name = n; css = c; break; }
        }

        int? until = null; string? next = null;
        foreach (var (need, n, _) in new[] { (3, "Bronze", ""), (5, "Silver", ""), (8, "Gold", ""), (12, "Emerald", ""), (15, "Diamond", ""), (24, "Master", "") })
        {
            if (months < need) { until = need - months; next = n; break; }
        }

        return new RankInfo
        {
            Name = name,
            BadgeClass = css,
            MonthsSubscribed = months,
            MonthsUntilNext = until,
            NextRankName = next
        };
    }

    private static int FullMonths(DateTime start, DateTime end)
    {
        if (end < start) return 0;
        var m = (end.Year - start.Year) * 12 + (end.Month - start.Month);
        if (end.Day < start.Day) m--;
        return Math.Max(0, m);
    }
}
