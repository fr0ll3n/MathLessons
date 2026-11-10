using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Level> Levels { get; set; } = null!;
    public DbSet<Topic> Topics { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Exercise> Exercises { get; set; } = null!;
    public DbSet<LessonProgress> LessonProgress { get; set; } = null!;
    public DbSet<Sub> Subs { get; set; } = null!;
    public DbSet<AppointmentPlan> AppointmentPlans { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Receipt> Receipts { get; set; } = null!;
    public DbSet<Schedule> Schedules { get; set; } = null!;
    public DbSet<SessionAttendance> SessionAttendances { get; set; } = null!;
    public DbSet<Certificate> Certificates { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(e =>
        {
            e.HasOne(u => u.Sub).WithMany(s => s.Users).HasForeignKey(u => u.SubId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<Topic>(e =>
        {
            e.HasOne(t => t.Level).WithMany(l => l.Topics).HasForeignKey(t => t.LevelId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Lesson>(e =>
        {
            e.HasOne(l => l.Level).WithMany(lv => lv.Lessons).HasForeignKey(l => l.LevelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Topic).WithMany(t => t.Lessons).HasForeignKey(l => l.TopicId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Exercise>(e =>
        {
            e.HasOne(x => x.Lesson).WithMany(l => l.Exercises).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.Appointments).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.AppointmentPlan).WithMany(p => p.Appointments).HasForeignKey(a => a.AppointmentPlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Receipt).WithMany(r => r.Appointments).HasForeignKey(a => a.ReceiptId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<LessonProgress>(e =>
        {
            e.HasOne(p => p.User).WithMany(u => u.LessonProgressEntries).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Lesson).WithMany(l => l.ProgressEntries).HasForeignKey(p => p.LessonId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
        });
        builder.Entity<Certificate>(e =>
        {
            e.HasOne(c => c.User).WithMany(u => u.Certificates).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Level).WithMany(l => l.Certificates).HasForeignKey(c => c.LevelId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => new { c.UserId, c.LevelId }).IsUnique();
        });
        builder.Entity<SessionAttendance>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.SessionAttendances).HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Schedule).WithMany(s => s.Attendances).HasForeignKey(a => a.ScheduleId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.UserId, a.ScheduleId }).IsUnique();
        });
    }
}
