using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _ctx;

    public NotificationService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task NotifyAsync(string userId, string title, string body, string? link = null)
    {
        _ctx.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Link = link
        });
        await _ctx.SaveChangesAsync();
    }

    public Task<int> UnreadCountAsync(string userId) =>
        _ctx.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task NotifySubscribersAboutContentAsync(string title, string body, string? link = null)
    {
        var userIds = await _ctx.Users
            .Where(u => u.SubId != null)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var id in userIds)
        {
            _ctx.Notifications.Add(new Notification
            {
                UserId = id,
                Title = title,
                Body = body,
                Link = link
            });
        }
        if (userIds.Count > 0)
            await _ctx.SaveChangesAsync();
    }
}
