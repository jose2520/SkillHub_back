using OfficeOpenXml;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;

namespace SkillHub.Infrastructure.Services;

public sealed class ExcelImportService : IExcelImportService
{
    public ExcelImportService()
    {
        ExcelPackage.License.SetNonCommercialPersonal("SkillHub");
    }

    public IReadOnlyList<ExcelEnrollmentRowDto> ParseEnrollmentRows(Stream excelStream)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        ExcelPackage package;
        try
        {
            package = new ExcelPackage(excelStream);
        }
        catch (Exception)
        {
            throw new ValidationException("file", "The uploaded file is not a valid Excel (.xlsx) workbook.");
        }

        using (package)
        {
            if (package.Workbook.Worksheets.Count == 0)
            {
                throw new ValidationException("file", "The Excel workbook does not contain any worksheets.");
            }

            var worksheet = ResolveTargetWorksheet(package.Workbook)
                ?? throw new ValidationException(
                    "file",
                    "Could not find an 'Enrollments' sheet or columns 'ParticipantEmail' and 'WorkshopCode'.");

            if (worksheet.Dimension is null || worksheet.Dimension.End.Row < 2)
            {
                return Array.Empty<ExcelEnrollmentRowDto>();
            }

            var (emailCol, codeCol) = ResolveColumnIndices(worksheet);
            var rows = new List<ExcelEnrollmentRowDto>();

            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var email = worksheet.Cells[row, emailCol].Text?.Trim() ?? string.Empty;
                var code = worksheet.Cells[row, codeCol].Text?.Trim() ?? string.Empty;

                // Ignore completely blank trailing rows in Excel
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(code))
                    continue;

                rows.Add(new ExcelEnrollmentRowDto(
                    RowNumber: row,
                    ParticipantEmail: email,
                    WorkshopCode: code));
            }

            return rows;
        }
    }

    private static ExcelWorksheet? ResolveTargetWorksheet(ExcelWorkbook workbook)
    {
        var namedSheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, "Enrollments", StringComparison.OrdinalIgnoreCase));

        if (namedSheet is not null)
            return namedSheet;

        foreach (var ws in workbook.Worksheets)
        {
            if (ws.Dimension is null)
                continue;

            var hasEmailHeader = false;
            var hasCodeHeader = false;

            for (var col = 1; col <= ws.Dimension.End.Column; col++)
            {
                var header = ws.Cells[1, col].Text?.Trim();
                if (string.Equals(header, "ParticipantEmail", StringComparison.OrdinalIgnoreCase))
                    hasEmailHeader = true;
                if (string.Equals(header, "WorkshopCode", StringComparison.OrdinalIgnoreCase))
                    hasCodeHeader = true;
            }

            if (hasEmailHeader && hasCodeHeader)
                return ws;
        }

        return null;
    }

    private static (int EmailCol, int CodeCol) ResolveColumnIndices(ExcelWorksheet worksheet)
    {
        int emailCol = 1;
        int codeCol = 2;

        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text?.Trim();
            if (string.Equals(header, "ParticipantEmail", StringComparison.OrdinalIgnoreCase))
                emailCol = col;
            else if (string.Equals(header, "WorkshopCode", StringComparison.OrdinalIgnoreCase))
                codeCol = col;
        }

        return (emailCol, codeCol);
    }
}
