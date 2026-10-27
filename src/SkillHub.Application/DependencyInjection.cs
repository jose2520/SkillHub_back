using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SkillHub.Application.Interfaces;
using SkillHub.Application.Services;

namespace SkillHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IWorkshopService, WorkshopService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
