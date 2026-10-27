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
