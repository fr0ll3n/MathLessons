namespace MathematicsLessons.Services.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string body, string? link = null);
    Task<int> UnreadCountAsync(string userId);

    /// <summary>
    /// Notify all subscribed students about catalog changes only
    /// (new/updated course, section, lesson fix — not chat/social).
    /// </summary>
    Task NotifySubscribersAboutContentAsync(string title, string body, string? link = null);
}
