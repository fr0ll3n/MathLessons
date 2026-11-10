using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

public interface ICertificateService
{
    Task<Certificate?> TryIssueForLevelAsync(string userId, int levelId);
    Task<List<Certificate>> GetForUserAsync(string userId);
}
