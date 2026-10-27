using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Infrastructure.Persistence.Seeders;

public sealed class DbSeeder : IDbSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        AppDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IDateTimeProvider dateTimeProvider,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;

        ExcelPackage.License.SetNonCommercialPersonal("SkillHub");
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            await _context.Database.MigrateAsync(cancellationToken);
        }

        await SeedRolesAsync();

        var excelPath = ResolveExcelFilePath();
        if (excelPath is null)
        {
            _logger.LogWarning("SkillHub_Data.xlsx was not found in expected paths. Skipping Excel initial seeding.");
            return;
        }

        using var package = new ExcelPackage(new FileInfo(excelPath));
        await SeedInstructorsAsync(package.Workbook.Worksheets["Instructors"], cancellationToken);
        await SeedParticipantsAsync(package.Workbook.Worksheets["Participants"], cancellationToken);
        await SeedWorkshopsAsync(package.Workbook.Worksheets["Workshops"], cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = [RoleNames.Employee, RoleNames.Participant];

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task SeedInstructorsAsync(ExcelWorksheet? sheet, CancellationToken cancellationToken)
    {
        if (sheet?.Dimension is null)
            return;

        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var fullName = sheet.Cells[row, 1].Text?.Trim();
            var rawEmail = sheet.Cells[row, 2].Text?.Trim();
            var password = sheet.Cells[row, 3].Text?.Trim();
            var specialty = sheet.Cells[row, 4].Text?.Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(rawEmail) || string.IsNullOrWhiteSpace(password))
                continue;

            var email = EmailAddress.Create(rawEmail).Value;
            var identityUser = await EnsureIdentityUserWithRoleAsync(email, password, RoleNames.Employee);

            if (!await _context.Instructors.AnyAsync(i => i.Email == email, cancellationToken))
            {
                var instructor = Instructor.Create(
                    identityUser.Id,
                    fullName,
                    email,
                    string.IsNullOrWhiteSpace(specialty) ? "General" : specialty,
                    _dateTimeProvider.UtcNow);

                await _context.Instructors.AddAsync(instructor, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task SeedParticipantsAsync(ExcelWorksheet? sheet, CancellationToken cancellationToken)
    {
        if (sheet?.Dimension is null)
            return;

        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var fullName = sheet.Cells[row, 1].Text?.Trim();
            var rawEmail = sheet.Cells[row, 2].Text?.Trim();
            var password = sheet.Cells[row, 3].Text?.Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(rawEmail) || string.IsNullOrWhiteSpace(password))
                continue;

            var email = EmailAddress.Create(rawEmail).Value;
            var identityUser = await EnsureIdentityUserWithRoleAsync(email, password, RoleNames.Participant);

            if (!await _context.Participants.AnyAsync(p => p.Email == email, cancellationToken))
            {
                var participant = Participant.Create(
                    identityUser.Id,
                    fullName,
                    email,
                    _dateTimeProvider.UtcNow);

                await _context.Participants.AddAsync(participant, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task SeedWorkshopsAsync(ExcelWorksheet? sheet, CancellationToken cancellationToken)
    {
        if (sheet?.Dimension is null)
            return;

        for (var row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var rawCode = sheet.Cells[row, 1].Text?.Trim();
            var title = sheet.Cells[row, 2].Text?.Trim();
            var description = sheet.Cells[row, 3].Text?.Trim();
            var rawInstructorEmail = sheet.Cells[row, 4].Text?.Trim();
            var capacityText = sheet.Cells[row, 7].Text?.Trim();

            if (string.IsNullOrWhiteSpace(rawCode) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(rawInstructorEmail))
                continue;

            var code = WorkshopCode.Create(rawCode).Value;
            if (await _context.Workshops.AnyAsync(w => w.Code == code, cancellationToken))
                continue;

            var instructorEmail = EmailAddress.Create(rawInstructorEmail).Value;
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Email == instructorEmail, cancellationToken);
            if (instructor is null)
            {
                _logger.LogWarning("Skipping workshop '{Code}': instructor '{Email}' not found.", code, instructorEmail);
                continue;
            }

            var startUtc = ParseExcelCellToUtc(sheet.Cells[row, 5]);
            var endUtc = ParseExcelCellToUtc(sheet.Cells[row, 6]);
            var capacity = int.TryParse(capacityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCap)
                ? parsedCap
                : sheet.Cells[row, 7].GetValue<int>();

            var workshop = Workshop.CreateSeeded(
                code,
                title,
                string.IsNullOrWhiteSpace(description) ? title : description,
                instructor.Id,
                startUtc,
                endUtc,
                capacity,
                _dateTimeProvider.UtcNow);

            await _context.Workshops.AddAsync(workshop, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IdentityUser> EnsureIdentityUserWithRoleAsync(string email, string password, string roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed Identity user '{email}': {errors}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            await _userManager.AddToRoleAsync(user, roleName);
        }

        return user;
    }

    private static DateTime ParseExcelCellToUtc(ExcelRange cell)
    {
        DateTime raw = cell.Value switch
        {
            DateTime dt => dt,
            double oaDate => DateTime.FromOADate(oaDate),
            string text when double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa) => DateTime.FromOADate(oa),
            string text => DateTime.Parse(text, CultureInfo.InvariantCulture),
            _ => cell.GetValue<DateTime>()
        };

        // Round to nearest second to eliminate floating-point OADate drift (e.g., 12:59:59.999 -> 13:00:00)
        var roundedTicks = (long)Math.Round(raw.Ticks / (double)TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
        return new DateTime(roundedTicks, DateTimeKind.Utc);
    }

    private static string? ResolveExcelFilePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Resources", "Data", "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Data", "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "SkillHub.Infrastructure", "Resources", "Data", "SkillHub_Data.xlsx"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "SkillHub.Infrastructure", "Resources", "Data", "SkillHub_Data.xlsx")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
