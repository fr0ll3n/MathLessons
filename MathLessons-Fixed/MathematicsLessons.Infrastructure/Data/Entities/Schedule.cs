using System.ComponentModel.DataAnnotations;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>
/// Public Q&A session — teacher explains exercises / hard course parts on Discord.
/// </summary>
public class Schedule
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime SelectedDateTime { get; set; }

    [Range(15, 480)]
    public int DurationMinutes { get; set; } = 60;

    public bool IsPublic { get; set; } = true;

    /// <summary>Discord server invite, e.g. https://discord.gg/xxxx</summary>
    [MaxLength(400)]
    public string? DiscordInviteUrl { get; set; }

    /// <summary>Channel name, e.g. #public-sessions</summary>
    [MaxLength(80)]
    public string? DiscordChannel { get; set; }

    [MaxLength(80)]
    public string? TopicFocus { get; set; }

    public ICollection<SessionAttendance> Attendances { get; set; } = new List<SessionAttendance>();
}
