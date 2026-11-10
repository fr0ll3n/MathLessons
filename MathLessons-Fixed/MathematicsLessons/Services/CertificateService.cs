using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Services;

public class CertificateService : ICertificateService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IProgressService _progress;
    private readonly INotificationService _notify;

    public CertificateService(ApplicationDbContext ctx, IProgressService progress, INotificationService notify)
    {
        _ctx = ctx;
        _progress = progress;
        _notify = notify;
    }

    public async Task<Certificate?> TryIssueForLevelAsync(string userId, int levelId)
    {
        if (!await _progress.IsLevelCompleteAsync(userId, levelId))
            return null;

        var existing = await _ctx.Certificates
            .FirstOrDefaultAsync(c => c.UserId == userId && c.LevelId == levelId);
        if (existing != null) return existing;

        var level = await _ctx.Levels.FindAsync(levelId);
        if (level == null) return null;

        var cert = new Certificate
        {
            UserId = userId,
            LevelId = levelId,
            Code = $"ML-{levelId}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
        };
        _ctx.Certificates.Add(cert);
        await _ctx.SaveChangesAsync();

        await _notify.NotifyAsync(userId, "Certificate earned",
            $"You completed {level.Name}. Code: {cert.Code}", "/Account/Profile");
        return cert;
    }

    public async Task<List<Certificate>> GetForUserAsync(string userId) =>
        await _ctx.Certificates.Include(c => c.Level)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
}
