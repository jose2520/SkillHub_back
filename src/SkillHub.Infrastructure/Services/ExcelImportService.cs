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

            var worksheet = ResolveTargetWorksheet(package.Workbook, out var emailCol, out var codeCol)
                ?? throw new ValidationException(
                    "file",
                    "Could not find an 'Enrollments' sheet or columns 'ParticipantEmail' and 'WorkshopCode'.");

            if (worksheet.Dimension is null || worksheet.Dimension.End.Row < 2)
            {
                return Array.Empty<ExcelEnrollmentRowDto>();
            }

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

    private static ExcelWorksheet? ResolveTargetWorksheet(
        ExcelWorkbook workbook,
        out int emailCol,
        out int codeCol)
    {
        var namedSheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, "Enrollments", StringComparison.OrdinalIgnoreCase));

        if (namedSheet is not null && TryResolveColumnIndices(namedSheet, out emailCol, out codeCol))
            return namedSheet;

        foreach (var ws in workbook.Worksheets)
        {
            if (TryResolveColumnIndices(ws, out emailCol, out codeCol))
                return ws;
        }

        emailCol = 0;
        codeCol = 0;
        return null;
    }

    private static bool TryResolveColumnIndices(ExcelWorksheet worksheet, out int emailCol, out int codeCol)
    {
        emailCol = 0;
        codeCol = 0;

        if (worksheet.Dimension is null)
            return false;

        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text?.Trim();
            if (emailCol == 0 && string.Equals(header, "ParticipantEmail", StringComparison.OrdinalIgnoreCase))
                emailCol = col;
            else if (codeCol == 0 && string.Equals(header, "WorkshopCode", StringComparison.OrdinalIgnoreCase))
                codeCol = col;
        }

        return emailCol > 0 && codeCol > 0;
    }
}
