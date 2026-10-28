using SkillHub.Application.DTOs;

namespace SkillHub.Application.Interfaces;

public interface IExcelImportService
{
    IReadOnlyList<ExcelEnrollmentRowDto> ParseEnrollmentRows(Stream excelStream);
}

public interface ICertificatePdfService
{
    byte[] GenerateEnrollmentCertificate(CertificateDataDto data);
}

public interface IExcelReportService
{
    byte[] GenerateEnrollmentImportTemplate(
        IReadOnlyList<WorkshopResponseDto> upcomingWorkshops,
        DateTime generatedAtUtc);

    byte[] GenerateTopWorkshopsExcelReport(
        IReadOnlyList<TopWorkshopReportDto> workshops,
        DateTime generatedAtUtc);
}
