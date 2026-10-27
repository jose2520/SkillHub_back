using System.Text;
using SkillHub.Application.DTOs;
using SkillHub.Infrastructure.Services;

namespace SkillHub.Tests.Infrastructure;

public sealed class ServicesIntegrationTests
{
    [Fact]
    public void ExcelImportService_ParsesOfficialSkillHubDataWorkbookEnrollmentsSheet()
    {
        var excelPath = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "Data",
            "SkillHub_Data.xlsx");

        Assert.True(File.Exists(excelPath), $"Expected SkillHub_Data.xlsx at {excelPath}");

        using var fileStream = File.OpenRead(excelPath);
        var sut = new ExcelImportService();

        var rows = sut.ParseEnrollmentRows(fileStream);

        Assert.Equal(70, rows.Count);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal("daniela.morales@correo.com", rows[0].ParticipantEmail);
        Assert.Equal("COC-001", rows[0].WorkshopCode);
    }

    [Fact]
    public void CertificatePdfService_GeneratesValidNonEmptyPdfBinary()
    {
        var sut = new CertificatePdfService();
        var data = new CertificateDataDto(
            EnrollmentId: Guid.NewGuid(),
            ParticipantFullName: "Jorge Andrés Pérez Rojas",
            ParticipantEmail: "jorge.perez@correo.com",
            WorkshopCode: "COC-001",
            WorkshopTitle: "Cocina costeña",
            InstructorFullName: "Rosa Elena Barrios Polo",
            WorkshopStartDateTime: new DateTime(2026, 12, 5, 14, 0, 0, DateTimeKind.Utc),
            WorkshopEndDateTime: new DateTime(2026, 12, 5, 18, 0, 0, DateTimeKind.Utc),
            EnrolledAtUtc: new DateTime(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc),
            GeneratedAtUtc: new DateTime(2026, 10, 27, 16, 0, 0, DateTimeKind.Utc));

        var pdfBytes = sut.GenerateEnrollmentCertificate(data);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
        var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
