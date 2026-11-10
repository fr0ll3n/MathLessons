using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;

namespace MathematicsLessons.Models;

public class ProfilePage
{
    public User User { get; set; } = null!;
    public int CompletedLessons { get; set; }
    public List<Certificate> Certificates { get; set; } = new();
    public RankInfo Rank { get; set; } = null!;
}
