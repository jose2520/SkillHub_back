using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;
using SkillHub.Infrastructure.Configuration;
using SkillHub.Infrastructure.Persistence;
using SkillHub.Infrastructure.Persistence.Repositories;
using SkillHub.Infrastructure.Persistence.Seeders;
using SkillHub.Infrastructure.Services;

namespace SkillHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.CommandTimeout(30);
            });
        });

        services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IWorkshopRepository, WorkshopRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IDbSeeder, DbSeeder>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IExcelReportService, ExcelReportService>();
        services.AddScoped<ICertificatePdfService, CertificatePdfService>();

        return services;
    }
}
