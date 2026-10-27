using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Domain.Enums;
using SkillHub.Infrastructure.Services;

namespace SkillHub.Tests.Infrastructure;

public sealed class ServicesIntegrationTests
{
    public ServicesIntegrationTests()
    {
        ExcelPackage.License.SetNonCommercialPersonal("SkillHub");
    }

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
    public void ExcelImportService_ParsesCustomSingleSheetWithReversedColumnsAndSkipsBlankRows()
    {
        using var ms = new MemoryStream();
        using (var package = new ExcelPackage())
        {
            var sheet = package.Workbook.Worksheets.Add("CustomSheet");
            sheet.Cells[1, 1].Value = "WorkshopCode";
            sheet.Cells[1, 2].Value = "ParticipantEmail";

            sheet.Cells[2, 1].Value = "FOT-001";
            sheet.Cells[2, 2].Value = "jorge.perez@correo.com";

            // Row 3 intentionally blank
            sheet.Cells[4, 1].Value = "COC-002";
            sheet.Cells[4, 2].Value = "luz.ospina@correo.com";

            package.SaveAs(ms);
        }

        ms.Position = 0;
        var sut = new ExcelImportService();
        var rows = sut.ParseEnrollmentRows(ms);

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal("jorge.perez@correo.com", rows[0].ParticipantEmail);
        Assert.Equal("FOT-001", rows[0].WorkshopCode);
        Assert.Equal(4, rows[1].RowNumber);
        Assert.Equal("luz.ospina@correo.com", rows[1].ParticipantEmail);
    }

    [Fact]
    public void ExcelImportService_InvalidOrHeaderlessWorkbook_ThrowsValidationException()
    {
        var sut = new ExcelImportService();

        using var corrupted = new MemoryStream(Encoding.UTF8.GetBytes("not-a-zip-excel-file"));
        Assert.Throws<ValidationException>(() => sut.ParseEnrollmentRows(corrupted));

        using var missingHeadersStream = new MemoryStream();
        using (var package = new ExcelPackage())
        {
            var sheet = package.Workbook.Worksheets.Add("WrongSheet");
            sheet.Cells[1, 1].Value = "Name";
            sheet.Cells[1, 2].Value = "Age";
            package.SaveAs(missingHeadersStream);
        }

        missingHeadersStream.Position = 0;
        Assert.Throws<ValidationException>(() => sut.ParseEnrollmentRows(missingHeadersStream));

        using var wrongHeadersNamedSheetStream = new MemoryStream();
        using (var package = new ExcelPackage())
        {
            var sheet = package.Workbook.Worksheets.Add("Enrollments");
            sheet.Cells[1, 1].Value = "WorkshopCode";
            sheet.Cells[1, 2].Value = "WrongEmailColumn";
            package.SaveAs(wrongHeadersNamedSheetStream);
        }

        wrongHeadersNamedSheetStream.Position = 0;
        Assert.Throws<ValidationException>(() => sut.ParseEnrollmentRows(wrongHeadersNamedSheetStream));
    }

    [Fact]
    public void CertificatePdfService_GeneratesPdfContainingAllRequiredCertificateFields()
    {
        var sut = new CertificatePdfService();
        var data = new CertificateDataDto(
            EnrollmentId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
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
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));

        using var reader = new PdfReader(new MemoryStream(pdfBytes));
        using var pdfDoc = new PdfDocument(reader);
        Assert.Equal(1, pdfDoc.GetNumberOfPages());

        var extractedText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetFirstPage());
        Assert.Contains("SkillHub", extractedText);
        Assert.Contains("Constancia de inscripción", extractedText);
        Assert.Contains("Jorge Andrés Pérez Rojas", extractedText);
        Assert.Contains("jorge.perez@correo.com", extractedText);
        Assert.Contains("COC-001", extractedText);
        Assert.Contains("Cocina costeña", extractedText);
        Assert.Contains("Rosa Elena Barrios Polo", extractedText);
        Assert.Contains("2026-12-05 14:00 - 2026-12-05 18:00 UTC", extractedText);
        Assert.Contains("2026-10-27 15:00:00 UTC", extractedText);
        Assert.Contains("2026-10-27 16:00:00 UTC", extractedText);
    }

    [Fact]
    public void JwtTokenService_GeneratesSignedTokenWithExpectedClaimsAndExpiry()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "UnitTest_JwtSigningKey_2026_Min32BytesLong!",
                ["Jwt:Issuer"] = "SkillHubAPI",
                ["Jwt:Audience"] = "SkillHubClient",
                ["Jwt:ExpiryMinutes"] = "90"
            })
            .Build();

        var clock = new SystemDateTimeProvider();
        var sut = new JwtTokenService(config, clock);
        var profileId = Guid.NewGuid();

        var (tokenString, expiresAtUtc) = sut.GenerateToken(
            "identity-123",
            profileId,
            "Andrés Mejía",
            "andres.mejia@correo.com",
            RoleNames.Employee);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        Assert.Equal("SkillHubAPI", jwt.Issuer);
        Assert.Contains("SkillHubClient", jwt.Audiences);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "identity-123");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == "andres.mejia@correo.com");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.Employee);
        Assert.Contains(jwt.Claims, c => c.Type == "profileId" && c.Value == profileId.ToString());
        Assert.True(expiresAtUtc > DateTime.UtcNow.AddMinutes(85));
    }
}
