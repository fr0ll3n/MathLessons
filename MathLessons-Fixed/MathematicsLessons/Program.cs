using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Large video uploads (total library can grow toward ~70 GB on disk)
        const long maxUpload = 2L * 1024 * 1024 * 1024; // 2 GB per request/file (diploma-friendly)
        builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
        {
            o.MultipartBodyLengthLimit = 100L * 1024 * 1024 * 1024;
            o.ValueLengthLimit = int.MaxValue;
            o.MultipartHeadersLengthLimit = int.MaxValue;
            o.MemoryBufferThreshold = 1024 * 1024;
        });
        builder.WebHost.ConfigureKestrel(o =>
        {
            o.Limits.MaxRequestBodySize = 100L * 1024 * 1024 * 1024;
            o.Limits.KeepAliveTimeout = TimeSpan.FromHours(6);
            o.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
        });


        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDefaultIdentity<User>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(o =>
        {
            o.IdleTimeout = TimeSpan.FromMinutes(30);
            o.Cookie.HttpOnly = true;
            o.Cookie.IsEssential = true;
        });

        // Course-platform services (interfaces)
        builder.Services.AddScoped<ILessonAccessService, LessonAccessService>();
        builder.Services.AddScoped<IProgressService, ProgressService>();
        builder.Services.AddScoped<ICertificateService, CertificateService>();
        builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IPointsService, PointsService>();
        builder.Services.AddScoped<ISubscriberRankService, SubscriberRankService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[] { "Student", "Teacher", "Admin" })
            {
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole(role));
            }

            // Owner: config OwnerEmail or first registered user becomes Admin+Teacher
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var ownerEmail = app.Configuration["OwnerEmail"];
            User? owner = null;
            if (!string.IsNullOrWhiteSpace(ownerEmail))
                owner = await userMgr.FindByEmailAsync(ownerEmail);
            if (owner == null)
                owner = await userMgr.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();
            if (owner != null)
            {
                if (!await userMgr.IsInRoleAsync(owner, "Admin"))
                    await userMgr.AddToRoleAsync(owner, "Admin");
                if (!await userMgr.IsInRoleAsync(owner, "Teacher"))
                    await userMgr.AddToRoleAsync(owner, "Teacher");
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();
        app.Run();
    }
}
