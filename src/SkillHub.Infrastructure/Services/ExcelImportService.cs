using OfficeOpenXml;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;

namespace SkillHub.Infrastructure.Services;

public sealed class ExcelImportService : IExcelImportService
{
    private const int MaxHeaderSearchRow = 15;

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

            var worksheet = ResolveTargetWorksheet(package.Workbook, out var headerRow, out var emailCol, out var codeCol)
                ?? throw new ValidationException(
                    "file",
                    "Could not find an 'Enrollments' sheet or columns 'ParticipantEmail' and 'WorkshopCode'.");

            if (worksheet.Dimension is null || worksheet.Dimension.End.Row <= headerRow)
            {
                return Array.Empty<ExcelEnrollmentRowDto>();
            }

            var rows = new List<ExcelEnrollmentRowDto>();

            for (var row = headerRow + 1; row <= worksheet.Dimension.End.Row; row++)
            {
                var email = worksheet.Cells[row, emailCol].Text?.Trim() ?? string.Empty;
                var code = worksheet.Cells[row, codeCol].Text?.Trim() ?? string.Empty;

                // Ignore completely blank rows
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
        out int headerRow,
        out int emailCol,
        out int codeCol)
    {
        var namedSheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, "Enrollments", StringComparison.OrdinalIgnoreCase));

        if (namedSheet is not null && TryResolveHeaderPosition(namedSheet, out headerRow, out emailCol, out codeCol))
            return namedSheet;

        foreach (var ws in workbook.Worksheets)
        {
            if (TryResolveHeaderPosition(ws, out headerRow, out emailCol, out codeCol))
                return ws;
        }

        headerRow = 0;
        emailCol = 0;
        codeCol = 0;
        return null;
    }

    private static bool TryResolveHeaderPosition(
        ExcelWorksheet worksheet,
        out int headerRow,
        out int emailCol,
        out int codeCol)
    {
        headerRow = 0;
        emailCol = 0;
        codeCol = 0;

        if (worksheet.Dimension is null)
            return false;

        var maxRowToScan = Math.Min(MaxHeaderSearchRow, worksheet.Dimension.End.Row);

        for (var r = 1; r <= maxRowToScan; r++)
        {
            int foundEmail = 0;
            int foundCode = 0;

            for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[r, col].Text?.Trim();
                if (foundEmail == 0 && string.Equals(header, "ParticipantEmail", StringComparison.OrdinalIgnoreCase))
                    foundEmail = col;
                else if (foundCode == 0 && string.Equals(header, "WorkshopCode", StringComparison.OrdinalIgnoreCase))
                    foundCode = col;
            }

            if (foundEmail > 0 && foundCode > 0)
            {
                headerRow = r;
                emailCol = foundEmail;
                codeCol = foundCode;
                return true;
            }
        }

        return false;
    }
}
