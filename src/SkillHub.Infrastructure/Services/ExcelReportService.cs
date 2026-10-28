using System.Drawing;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;

namespace SkillHub.Infrastructure.Services;

public sealed class ExcelReportService : IExcelReportService
{
    // Official SkillHub Corporate Palette
    private static readonly Color BrandNavy = Color.FromArgb(11, 16, 33);           // #0B1021 Midnight Academic Navy
    private static readonly Color BrandViolet = Color.FromArgb(109, 40, 217);       // #6D28D9 SkillHub Electric Violet
    private static readonly Color BrandVioletSoft = Color.FromArgb(245, 243, 255);  // #F5F3FF Soft Violet Banner
    private static readonly Color BrandBlue = Color.FromArgb(37, 99, 235);          // #2563EB SkillHub Royal Blue
    private static readonly Color BrandAmber = Color.FromArgb(245, 158, 11);        // #F59E0B SkillHub Play Gold
    private static readonly Color BrandAmberSoft = Color.FromArgb(255, 251, 235);   // #FFFBEB Soft Amber Totals
    private static readonly Color SlateMuted = Color.FromArgb(71, 85, 105);         // #475569 Slate Label
    private static readonly Color SlateGray = Color.FromArgb(100, 116, 139);        // #64748B Muted Note
    private static readonly Color LightBorder = Color.FromArgb(226, 232, 240);      // #E2E8F0 Clean Cell Border
    private static readonly Color ZebraRow = Color.FromArgb(248, 250, 252);         // #F8FAFC Alternating Row
    private static readonly Color SurfaceCard = Color.FromArgb(241, 245, 249);      // #F1F5F9 KPI Label Surface

    // Status Pill Badges
    private static readonly Color BadgeGreenBg = Color.FromArgb(220, 252, 231);     // #DCFCE7
    private static readonly Color BadgeGreenText = Color.FromArgb(22, 101, 52);     // #166534
    private static readonly Color BadgeAmberBg = Color.FromArgb(254, 243, 199);     // #FEF3C7
    private static readonly Color BadgeAmberText = Color.FromArgb(146, 64, 14);     // #92400E
    private static readonly Color BadgeRedBg = Color.FromArgb(254, 226, 226);       // #FEE2E2
    private static readonly Color BadgeRedText = Color.FromArgb(153, 27, 27);       // #991B1B

    private const string IntegerFormat = "#,##0";
    private const string PercentFormat = "0.0%";

    public ExcelReportService()
    {
        ExcelPackage.License.SetNonCommercialPersonal("SkillHub");
    }

    public byte[] GenerateTopWorkshopsExcelReport(
        IReadOnlyList<TopWorkshopReportDto> workshops,
        DateTime generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(workshops);

        using var package = new ExcelPackage();

        // =================================================================================
        // SHEET 1: TOP WORKSHOPS DETAILED REPORT
        // =================================================================================
        var ws = package.Workbook.Worksheets.Add("Top Workshops");
        ws.TabColor = BrandNavy;
        ConfigureSheetPageSetup(ws);

        InsertHorizontalLogo(ws, "SkillHubLogoTopWorkshops", fromRow: 0, rowOffsetPx: 6, fromCol: 0, colOffsetPx: 10);

        RenderBannerHeader(
            ws,
            endCol: 11,
            title: "                            SKILLHUB   |   TOP WORKSHOPS BY ACTIVE ENROLLMENTS",
            subtitle: $"Official Report Issued: {generatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC   •   Total Workshops: {workshops.Count}   •   Active Seats Occupied: {workshops.Sum(w => w.ActiveEnrollments)}   •   SkillHub Academic Platform");

        string[] headers =
        [
            "Rank",
            "Code",
            "Workshop Title",
            "Instructor",
            "Instructor Email",
            "Start (UTC)",
            "End (UTC)",
            "Capacity",
            "Active Enrolled",
            "Available Spots",
            "Seat Status"
        ];

        ws.Row(4).Height = 28;
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cells[4, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Name = "Segoe UI";
            cell.Style.Font.Size = 10f;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(BrandNavy);
            cell.Style.HorizontalAlignment = i is >= 7 and <= 9
                ? ExcelHorizontalAlignment.Right
                : i is 2 or 3 or 4
                    ? ExcelHorizontalAlignment.Left
                    : ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(51, 65, 85));
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            cell.Style.Border.Bottom.Color.SetColor(BrandAmber);
        }

        const int startDataRow = 5;
        int row = startDataRow;

        if (workshops.Count == 0)
        {
            ws.Cells[row, 1, row, 11].Merge = true;
            ws.Cells[row, 1].Value = "No workshops are currently registered in SkillHub.";
            ws.Cells[row, 1].Style.Font.Name = "Segoe UI";
            ws.Cells[row, 1].Style.Font.Italic = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(SlateGray);
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Row(row).Height = 26;
            ws.Cells[row, 1, row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
            row++;
        }
        else
        {
            int rank = 1;
            foreach (var w in workshops)
            {
                ws.Row(row).Height = 22;
                var rowBg = (row % 2 == 0) ? Color.White : ZebraRow;

                ws.Cells[row, 1].Value = rank;
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, 2].Value = w.Code;
                ws.Cells[row, 2].Style.Font.Bold = true;
                ws.Cells[row, 2].Style.Font.Color.SetColor(BrandViolet);
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, 3].Value = w.Title;
                ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                ws.Cells[row, 4].Value = w.InstructorName;
                ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                ws.Cells[row, 5].Value = w.InstructorEmail;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                ws.Cells[row, 6].Value = w.StartDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, 7].Value = w.EndDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, 8].Value = w.Capacity;
                ws.Cells[row, 8].Style.Numberformat.Format = IntegerFormat;
                ws.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws.Cells[row, 9].Value = w.ActiveEnrollments;
                ws.Cells[row, 9].Style.Numberformat.Format = IntegerFormat;
                ws.Cells[row, 9].Style.Font.Bold = true;
                ws.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws.Cells[row, 10].Value = w.AvailableSpots;
                ws.Cells[row, 10].Style.Numberformat.Format = IntegerFormat;
                ws.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                var occupancyRatio = w.Capacity > 0 ? (double)w.ActiveEnrollments / w.Capacity : 0.0;
                var statusCell = ws.Cells[row, 11];
                ApplyOccupancyStatusBadge(statusCell, w.AvailableSpots, occupancyRatio);

                for (int col = 1; col <= 11; col++)
                {
                    var cell = ws.Cells[row, col];
                    cell.Style.Font.Name = "Segoe UI";
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    if (col != 11)
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(rowBg);
                    }
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
                }

                rank++;
                row++;
            }

            // Summary Totals Row
            int totalRow = row;
            ws.Row(totalRow).Height = 26;
            ws.Cells[totalRow, 1, totalRow, 7].Merge = true;
            ws.Cells[totalRow, 1].Value = "CONSOLIDATED ACADEMIC TOTALS";
            ws.Cells[totalRow, 1].Style.Font.Bold = true;
            ws.Cells[totalRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[totalRow, 8].Formula = $"SUM(H{startDataRow}:H{totalRow - 1})";
            ws.Cells[totalRow, 8].Style.Numberformat.Format = IntegerFormat;
            ws.Cells[totalRow, 8].Style.Font.Bold = true;
            ws.Cells[totalRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[totalRow, 9].Formula = $"SUM(I{startDataRow}:I{totalRow - 1})";
            ws.Cells[totalRow, 9].Style.Numberformat.Format = IntegerFormat;
            ws.Cells[totalRow, 9].Style.Font.Bold = true;
            ws.Cells[totalRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[totalRow, 10].Formula = $"SUM(J{startDataRow}:J{totalRow - 1})";
            ws.Cells[totalRow, 10].Style.Numberformat.Format = IntegerFormat;
            ws.Cells[totalRow, 10].Style.Font.Bold = true;
            ws.Cells[totalRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[totalRow, 11].Value = $"{workshops.Count(w => w.AvailableSpots > 0)} Open";
            ws.Cells[totalRow, 11].Style.Font.Bold = true;
            ws.Cells[totalRow, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            StyleTotalRow(ws, totalRow, startCol: 1, endCol: 11);

            ws.Cells[4, 1, totalRow - 1, 11].AutoFilter = true;

            // Executive KPI Cards below table
            int kpiLabelRow = totalRow + 2;
            int kpiValStart = totalRow + 3;
            int kpiValEnd = totalRow + 4;
            ws.Row(totalRow + 1).Height = 14;
            ws.Row(kpiLabelRow).Height = 18;
            ws.Row(kpiValStart).Height = 20;
            ws.Row(kpiValEnd).Height = 20;

            int soldOutCount = workshops.Count(w => w.AvailableSpots <= 0);
            RenderKpiCard(ws, kpiLabelRow, kpiValStart, kpiValEnd, 2, 3, "PUBLISHED WORKSHOPS", $"{workshops.Count} Workshops", "", BrandViolet);
            RenderKpiCard(ws, kpiLabelRow, kpiValStart, kpiValEnd, 4, 6, "TOTAL SEAT CAPACITY", $"=SUM(H{startDataRow}:H{totalRow - 1})", IntegerFormat, BrandBlue);
            RenderKpiCard(ws, kpiLabelRow, kpiValStart, kpiValEnd, 7, 9, "ACTIVE ENROLLMENTS", $"=SUM(I{startDataRow}:I{totalRow - 1})", IntegerFormat, BrandAmber);
            RenderKpiCard(ws, kpiLabelRow, kpiValStart, kpiValEnd, 10, 11, "FULL WORKSHOPS", $"{soldOutCount} Full", "", soldOutCount > 0 ? BadgeRedText : BrandNavy);
        }

        ws.View.FreezePanes(5, 1);
        ws.Cells[4, 1, Math.Max(4, row), 11].AutoFitColumns();
        for (int col = 1; col <= 11; col++)
        {
            ws.Column(col).Width = Math.Max(ws.Column(col).Width + 4, 12);
        }

        // =================================================================================
        // SHEET 2: EXECUTIVE OCCUPANCY DASHBOARD
        // =================================================================================
        if (workshops.Count > 0)
        {
            BuildExecutiveDashboardSheet(package, workshops, generatedAtUtc);
        }

        return package.GetAsByteArray();
    }

    public byte[] GenerateEnrollmentImportTemplate(
        IReadOnlyList<WorkshopResponseDto> upcomingWorkshops,
        DateTime generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(upcomingWorkshops);

        using var package = new ExcelPackage();

        // =================================================================================
        // SHEET 1: ENROLLMENTS BULK IMPORT TEMPLATE (Compatible with POST /api/v1/enrollments/import)
        // =================================================================================
        var ws = package.Workbook.Worksheets.Add("Enrollments");
        ws.TabColor = BrandViolet;
        ConfigureSheetPageSetup(ws);

        InsertHorizontalLogo(ws, "SkillHubLogoTemplate", fromRow: 0, rowOffsetPx: 6, fromCol: 0, colOffsetPx: 10);

        RenderBannerHeader(
            ws,
            endCol: 4,
            title: "                            SKILLHUB   |   BULK ENROLLMENT IMPORT TEMPLATE",
            subtitle: $"Template Generated: {generatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC   •   Fill 'ParticipantEmail' and 'WorkshopCode' below and upload to POST /api/v1/enrollments/import");

        // Column Headers in Row 4 (Automatically detected by ExcelImportService!)
        ws.Row(4).Height = 28;
        string[] templateHeaders = ["ParticipantEmail", "WorkshopCode", "Notes (Optional - Ignored)", "Validation Rules Summary"];
        for (int i = 0; i < templateHeaders.Length; i++)
        {
            var cell = ws.Cells[4, i + 1];
            cell.Value = templateHeaders[i];
            cell.Style.Font.Name = "Segoe UI";
            cell.Style.Font.Size = 10.5f;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(i < 2 ? BrandNavy : BrandViolet);
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(51, 65, 85));
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            cell.Style.Border.Bottom.Color.SetColor(BrandAmber);
        }

        var firstCode = upcomingWorkshops.FirstOrDefault()?.Code ?? "COC-001";
        var secondCode = upcomingWorkshops.Skip(1).FirstOrDefault()?.Code ?? "FOT-001";

        (string Email, string Code, string Note, string Rule)[] sampleRows =
        [
            ("jorge.perez@correo.com", firstCode, "Replace or add participant rows below", "1. Participant & Workshop must exist in SkillHub"),
            ("luz.ospina@correo.com", secondCode, "Valid rows save even if other rows fail", "2. Enforces future start, capacity, no duplicates & no time overlap")
        ];

        for (int idx = 0; idx < sampleRows.Length; idx++)
        {
            int r = 5 + idx;
            ws.Row(r).Height = 22;
            var bg = (r % 2 == 0) ? Color.White : ZebraRow;

            ws.Cells[r, 1].Value = sampleRows[idx].Email;
            ws.Cells[r, 2].Value = sampleRows[idx].Code;
            ws.Cells[r, 2].Style.Font.Bold = true;
            ws.Cells[r, 2].Style.Font.Color.SetColor(BrandViolet);
            ws.Cells[r, 3].Value = sampleRows[idx].Note;
            ws.Cells[r, 3].Style.Font.Italic = true;
            ws.Cells[r, 3].Style.Font.Color.SetColor(SlateGray);
            ws.Cells[r, 4].Value = sampleRows[idx].Rule;
            ws.Cells[r, 4].Style.Font.Color.SetColor(SlateMuted);

            for (int c = 1; c <= 4; c++)
            {
                var cell = ws.Cells[r, c];
                cell.Style.Font.Name = "Segoe UI";
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(bg);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
            }
        }

        // Pre-format blank entry rows 7..20 for smooth user typing
        for (int r = 7; r <= 20; r++)
        {
            ws.Row(r).Height = 21;
            var bg = (r % 2 == 0) ? Color.White : ZebraRow;
            for (int c = 1; c <= 3; c++)
            {
                var cell = ws.Cells[r, c];
                cell.Style.Font.Name = "Segoe UI";
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(bg);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
            }
        }

        ws.View.FreezePanes(5, 1);
        ws.Column(1).Width = 34;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 36;
        ws.Column(4).Width = 54;

        // =================================================================================
        // SHEET 2: AVAILABLE UPCOMING WORKSHOPS REFERENCE CATALOG
        // =================================================================================
        var wsRef = package.Workbook.Worksheets.Add("Available Workshops");
        wsRef.TabColor = BrandBlue;
        ConfigureSheetPageSetup(wsRef);

        InsertHorizontalLogo(wsRef, "SkillHubLogoRefCatalog", fromRow: 0, rowOffsetPx: 6, fromCol: 0, colOffsetPx: 10);

        RenderBannerHeader(
            wsRef,
            endCol: 7,
            title: "                            SKILLHUB   |   UPCOMING WORKSHOPS REFERENCE CATALOG",
            subtitle: $"Live Workshop Codes & Available Spots as of {generatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC   •   Copy any 'Code' into the Enrollments tab");

        string[] refHeaders = ["Code", "Workshop Title", "Instructor", "Start (UTC)", "End (UTC)", "Capacity", "Available Spots"];
        wsRef.Row(4).Height = 26;
        for (int i = 0; i < refHeaders.Length; i++)
        {
            var cell = wsRef.Cells[4, i + 1];
            cell.Value = refHeaders[i];
            cell.Style.Font.Name = "Segoe UI";
            cell.Style.Font.Size = 10f;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(BrandNavy);
            cell.Style.HorizontalAlignment = i >= 5 ? ExcelHorizontalAlignment.Right : i is 1 or 2 ? ExcelHorizontalAlignment.Left : ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            cell.Style.Border.Bottom.Color.SetColor(BrandAmber);
        }

        int refRow = 5;
        foreach (var w in upcomingWorkshops)
        {
            wsRef.Row(refRow).Height = 21;
            var bg = (refRow % 2 == 0) ? Color.White : ZebraRow;

            wsRef.Cells[refRow, 1].Value = w.Code;
            wsRef.Cells[refRow, 1].Style.Font.Bold = true;
            wsRef.Cells[refRow, 1].Style.Font.Color.SetColor(BrandViolet);
            wsRef.Cells[refRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            wsRef.Cells[refRow, 2].Value = w.Title;
            wsRef.Cells[refRow, 3].Value = w.InstructorName;
            wsRef.Cells[refRow, 4].Value = w.StartDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            wsRef.Cells[refRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            wsRef.Cells[refRow, 5].Value = w.EndDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            wsRef.Cells[refRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            wsRef.Cells[refRow, 6].Value = w.Capacity;
            wsRef.Cells[refRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            wsRef.Cells[refRow, 7].Value = w.AvailableSpots;
            wsRef.Cells[refRow, 7].Style.Font.Bold = true;
            wsRef.Cells[refRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            for (int c = 1; c <= 7; c++)
            {
                var cell = wsRef.Cells[refRow, c];
                cell.Style.Font.Name = "Segoe UI";
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(bg);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
            }

            refRow++;
        }

        wsRef.View.FreezePanes(5, 1);
        wsRef.Cells[4, 1, Math.Max(4, refRow), 7].AutoFitColumns();
        for (int c = 1; c <= 7; c++)
        {
            wsRef.Column(c).Width = Math.Max(wsRef.Column(c).Width + 4, 15);
        }

        return package.GetAsByteArray();
    }

    private static void BuildExecutiveDashboardSheet(
        ExcelPackage package,
        IReadOnlyList<TopWorkshopReportDto> workshops,
        DateTime generatedAtUtc)
    {
        var wsDash = package.Workbook.Worksheets.Add("Executive Summary");
        wsDash.TabColor = BrandViolet;
        ConfigureSheetPageSetup(wsDash);

        InsertHorizontalLogo(wsDash, "SkillHubLogoExecDash", fromRow: 0, rowOffsetPx: 6, fromCol: 1, colOffsetPx: 8);

        wsDash.Row(1).Height = 48;
        wsDash.Cells["B1:G1"].Merge = true;
        wsDash.Cells["B1"].Value = "                            SKILLHUB   |   EXECUTIVE OCCUPANCY & DEMAND DASHBOARD";
        wsDash.Cells["B1"].Style.Font.Name = "Segoe UI";
        wsDash.Cells["B1"].Style.Font.Size = 13.5f;
        wsDash.Cells["B1"].Style.Font.Bold = true;
        wsDash.Cells["B1"].Style.Font.Color.SetColor(BrandNavy);
        wsDash.Cells["B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        wsDash.Cells["B1"].Style.Fill.BackgroundColor.SetColor(Color.White);
        wsDash.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        wsDash.Cells["B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        wsDash.Cells["B1:G1"].Style.Border.Top.Style = ExcelBorderStyle.Medium;
        wsDash.Cells["B1:G1"].Style.Border.Top.Color.SetColor(BrandNavy);

        wsDash.Row(2).Height = 5;
        wsDash.Cells["B2:G2"].Merge = true;
        wsDash.Cells["B2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        wsDash.Cells["B2"].Style.Fill.BackgroundColor.SetColor(BrandViolet);

        int totalCap = workshops.Sum(w => w.Capacity);
        int totalActive = workshops.Sum(w => w.ActiveEnrollments);
        int totalAvailable = workshops.Sum(w => w.AvailableSpots);
        double globalOccupancy = totalCap > 0 ? (double)totalActive / totalCap : 0.0;

        wsDash.Row(4).Height = 18;
        wsDash.Row(5).Height = 20;
        wsDash.Row(6).Height = 20;

        RenderKpiCard(wsDash, 4, 5, 6, 2, 3, "TOTAL PUBLISHED WORKSHOPS", $"{workshops.Count} Workshops", "", BrandViolet);
        RenderKpiCard(wsDash, 4, 5, 6, 4, 4, "TOTAL SEAT CAPACITY", totalCap.ToString(IntegerFormat, CultureInfo.InvariantCulture), "", BrandBlue);
        RenderKpiCard(wsDash, 4, 5, 6, 5, 5, "ACTIVE ENROLLMENTS", totalActive.ToString(IntegerFormat, CultureInfo.InvariantCulture), "", BrandAmber);
        RenderKpiCard(wsDash, 4, 5, 6, 6, 7, "GLOBAL SEAT OCCUPANCY", $"{globalOccupancy:P1} ({totalAvailable} Free)", "", BrandNavy);

        wsDash.Row(8).Height = 14;
        wsDash.Row(9).Height = 25;

        string[] tierHeaders = ["Demand Tier", "Occupancy Threshold", "Workshops", "% Portfolio", "Active Enrolled", "Available Spots"];
        for (int i = 0; i < tierHeaders.Length; i++)
        {
            var cell = wsDash.Cells[9, i + 2];
            cell.Value = tierHeaders[i];
            cell.Style.Font.Name = "Segoe UI";
            cell.Style.Font.Size = 9.5f;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(BrandNavy);
            cell.Style.HorizontalAlignment = i >= 2 ? ExcelHorizontalAlignment.Right : ExcelHorizontalAlignment.Left;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            cell.Style.Border.Bottom.Color.SetColor(BrandAmber);
        }

        var tiers = new[]
        {
            new { Name = "Open Availability", Rule = "Occupancy below 50%", Items = workshops.Where(w => w.Capacity > 0 && (double)w.ActiveEnrollments / w.Capacity < 0.50).ToList() },
            new { Name = "Steady Demand", Rule = "Occupancy between 50% and 79%", Items = workshops.Where(w => w.Capacity > 0 && (double)w.ActiveEnrollments / w.Capacity >= 0.50 && (double)w.ActiveEnrollments / w.Capacity < 0.80).ToList() },
            new { Name = "High Demand (Filling Fast)", Rule = "Occupancy between 80% and 99%", Items = workshops.Where(w => w.Capacity > 0 && (double)w.ActiveEnrollments / w.Capacity >= 0.80 && w.AvailableSpots > 0).ToList() },
            new { Name = "Sold Out (Full Capacity)", Rule = "0 available spots remaining", Items = workshops.Where(w => w.AvailableSpots <= 0).ToList() }
        };

        int dashRow = 10;
        foreach (var tier in tiers)
        {
            wsDash.Row(dashRow).Height = 22;
            var bg = (dashRow % 2 == 0) ? Color.White : ZebraRow;

            wsDash.Cells[dashRow, 2].Value = tier.Name;
            wsDash.Cells[dashRow, 2].Style.Font.Bold = true;
            wsDash.Cells[dashRow, 3].Value = tier.Rule;
            wsDash.Cells[dashRow, 4].Value = tier.Items.Count;
            wsDash.Cells[dashRow, 4].Style.Numberformat.Format = IntegerFormat;
            wsDash.Cells[dashRow, 5].Value = workshops.Count > 0 ? (double)tier.Items.Count / workshops.Count : 0.0;
            wsDash.Cells[dashRow, 5].Style.Numberformat.Format = PercentFormat;
            wsDash.Cells[dashRow, 6].Value = tier.Items.Sum(x => x.ActiveEnrollments);
            wsDash.Cells[dashRow, 6].Style.Numberformat.Format = IntegerFormat;
            wsDash.Cells[dashRow, 7].Value = tier.Items.Sum(x => x.AvailableSpots);
            wsDash.Cells[dashRow, 7].Style.Numberformat.Format = IntegerFormat;

            for (int c = 2; c <= 7; c++)
            {
                var cell = wsDash.Cells[dashRow, c];
                cell.Style.Font.Name = "Segoe UI";
                cell.Style.Font.Size = 9.5f;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(bg);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
            }

            dashRow++;
        }

        wsDash.Row(dashRow).Height = 25;
        wsDash.Cells[dashRow, 2, dashRow, 3].Merge = true;
        wsDash.Cells[dashRow, 2].Value = "CONSOLIDATED PORTFOLIO";
        wsDash.Cells[dashRow, 2].Style.Font.Bold = true;
        wsDash.Cells[dashRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        wsDash.Cells[dashRow, 4].Value = workshops.Count;
        wsDash.Cells[dashRow, 4].Style.Font.Bold = true;
        wsDash.Cells[dashRow, 5].Value = 1.0;
        wsDash.Cells[dashRow, 5].Style.Numberformat.Format = PercentFormat;
        wsDash.Cells[dashRow, 5].Style.Font.Bold = true;
        wsDash.Cells[dashRow, 6].Value = totalActive;
        wsDash.Cells[dashRow, 6].Style.Font.Bold = true;
        wsDash.Cells[dashRow, 7].Value = totalAvailable;
        wsDash.Cells[dashRow, 7].Style.Font.Bold = true;

        StyleTotalRow(wsDash, dashRow, startCol: 2, endCol: 7);

        int noteRow = dashRow + 2;
        wsDash.Cells[noteRow, 2, noteRow, 7].Merge = true;
        wsDash.Cells[noteRow, 2].Value = $"SkillHub  •  Aprende • Crea • Crece  •  Generated {generatedAtUtc:yyyy-MM-dd HH:mm} UTC";
        wsDash.Cells[noteRow, 2].Style.Font.Name = "Segoe UI";
        wsDash.Cells[noteRow, 2].Style.Font.Size = 8.5f;
        wsDash.Cells[noteRow, 2].Style.Font.Italic = true;
        wsDash.Cells[noteRow, 2].Style.Font.Color.SetColor(SlateGray);
        wsDash.Cells[noteRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        wsDash.Column(1).Width = 4;
        wsDash.Column(2).Width = 30;
        wsDash.Column(3).Width = 34;
        wsDash.Column(4).Width = 16;
        wsDash.Column(5).Width = 16;
        wsDash.Column(6).Width = 20;
        wsDash.Column(7).Width = 20;
    }

    private static void ConfigureSheetPageSetup(ExcelWorksheet ws)
    {
        ws.PrinterSettings.Orientation = eOrientation.Landscape;
        ws.PrinterSettings.PaperSize = ePaperSize.Letter;
        ws.PrinterSettings.FitToPage = true;
        ws.PrinterSettings.FitToWidth = 1;
        ws.PrinterSettings.FitToHeight = 0;
        ws.View.ShowGridLines = true;
    }

    private static void InsertHorizontalLogo(
        ExcelWorksheet ws,
        string pictureName,
        int fromRow,
        int rowOffsetPx,
        int fromCol,
        int colOffsetPx)
    {
        var logoPath = BrandAssetResolver.ResolveAssetPath(BrandAssetResolver.HorizontalLogoFileName);
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            try
            {
                // Exact 3.21:1 proportional dimensions matching 972x303 official logo
                var pic = ws.Drawings.AddPicture(pictureName, new FileInfo(logoPath));
                pic.SetPosition(fromRow, rowOffsetPx, fromCol, colOffsetPx);
                pic.SetSize(148, 46);
            }
            catch
            {
                // Gracefully continue if drawing surface is unavailable
            }
        }
    }

    private static void RenderBannerHeader(ExcelWorksheet ws, int endCol, string title, string subtitle)
    {
        ws.Row(1).Height = 48;
        ws.Cells[1, 1, 1, endCol].Merge = true;
        ws.Cells[1, 1].Value = title;
        ws.Cells[1, 1].Style.Font.Name = "Segoe UI";
        ws.Cells[1, 1].Style.Font.Size = 14f;
        ws.Cells[1, 1].Style.Font.Bold = true;
        wsDashStyleHeaderCell(ws.Cells[1, 1]);
        ws.Cells[1, 1, 1, endCol].Style.Border.Top.Style = ExcelBorderStyle.Medium;
        ws.Cells[1, 1, 1, endCol].Style.Border.Top.Color.SetColor(BrandNavy);

        ws.Row(2).Height = 5;
        ws.Cells[2, 1, 2, endCol].Merge = true;
        ws.Cells[2, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[2, 1].Style.Fill.BackgroundColor.SetColor(BrandViolet);

        ws.Row(3).Height = 24;
        ws.Cells[3, 1, 3, endCol].Merge = true;
        ws.Cells[3, 1].Value = subtitle;
        ws.Cells[3, 1].Style.Font.Name = "Segoe UI";
        ws.Cells[3, 1].Style.Font.Size = 9.5f;
        ws.Cells[3, 1].Style.Font.Italic = true;
        ws.Cells[3, 1].Style.Font.Color.SetColor(BrandNavy);
        ws.Cells[3, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[3, 1].Style.Fill.BackgroundColor.SetColor(BrandVioletSoft);
        ws.Cells[3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells[3, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        ws.Cells[3, 1, 3, endCol].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        ws.Cells[3, 1, 3, endCol].Style.Border.Bottom.Color.SetColor(BrandViolet);
    }

    private static void wsDashStyleHeaderCell(ExcelRange cell)
    {
        cell.Style.Font.Color.SetColor(BrandNavy);
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.White);
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    private static void ApplyOccupancyStatusBadge(ExcelRange cell, int availableSpots, double occupancyRatio)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.Size = 9f;
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

        if (availableSpots <= 0)
        {
            cell.Value = "FULL";
            cell.Style.Fill.BackgroundColor.SetColor(BadgeRedBg);
            cell.Style.Font.Color.SetColor(BadgeRedText);
        }
        else if (occupancyRatio >= 0.75)
        {
            cell.Value = "HIGH DEMAND";
            cell.Style.Fill.BackgroundColor.SetColor(BadgeAmberBg);
            cell.Style.Font.Color.SetColor(BadgeAmberText);
        }
        else
        {
            cell.Value = "OPEN";
            cell.Style.Fill.BackgroundColor.SetColor(BadgeGreenBg);
            cell.Style.Font.Color.SetColor(BadgeGreenText);
        }
    }

    private static void StyleTotalRow(ExcelWorksheet ws, int totalRow, int startCol, int endCol)
    {
        for (int col = startCol; col <= endCol; col++)
        {
            var c = ws.Cells[totalRow, col];
            c.Style.Font.Name = "Segoe UI";
            c.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            c.Style.Fill.PatternType = ExcelFillStyle.Solid;
            c.Style.Fill.BackgroundColor.SetColor(BrandAmberSoft);
            c.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            c.Style.Border.Top.Color.SetColor(BrandAmber);
            c.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
            c.Style.Border.Bottom.Color.SetColor(BrandNavy);
        }
    }

    private static void RenderKpiCard(
        ExcelWorksheet ws,
        int labelRow,
        int valStartRow,
        int valEndRow,
        int startCol,
        int endCol,
        string label,
        string formulaOrVal,
        string numFormat,
        Color accentColor)
    {
        ws.Cells[labelRow, startCol, labelRow, endCol].Merge = true;
        var lblCell = ws.Cells[labelRow, startCol];
        lblCell.Value = label;
        lblCell.Style.Font.Name = "Segoe UI";
        lblCell.Style.Font.Size = 8f;
        lblCell.Style.Font.Bold = true;
        lblCell.Style.Font.Color.SetColor(SlateMuted);
        lblCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        lblCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        lblCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        lblCell.Style.Fill.BackgroundColor.SetColor(SurfaceCard);

        ws.Cells[valStartRow, startCol, valEndRow, endCol].Merge = true;
        var valCell = ws.Cells[valStartRow, startCol];
        if (formulaOrVal.StartsWith('='))
            valCell.Formula = formulaOrVal[1..];
        else
            valCell.Value = formulaOrVal;

        valCell.Style.Font.Name = "Segoe UI";
        valCell.Style.Font.Size = 13f;
        valCell.Style.Font.Bold = true;
        valCell.Style.Font.Color.SetColor(BrandNavy);
        valCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        valCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        valCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        valCell.Style.Fill.BackgroundColor.SetColor(Color.White);

        if (!string.IsNullOrEmpty(numFormat))
            valCell.Style.Numberformat.Format = numFormat;

        for (int r = labelRow; r <= valEndRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                ws.Cells[r, c].Style.Border.BorderAround(ExcelBorderStyle.Thin, LightBorder);
            }
        }

        for (int c = startCol; c <= endCol; c++)
        {
            ws.Cells[labelRow, c].Style.Border.Top.Style = ExcelBorderStyle.Medium;
            ws.Cells[labelRow, c].Style.Border.Top.Color.SetColor(accentColor);
        }
    }
}
