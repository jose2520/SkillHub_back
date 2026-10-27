namespace SkillHub.Infrastructure.Persistence.Seeders;

public interface IDbSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
