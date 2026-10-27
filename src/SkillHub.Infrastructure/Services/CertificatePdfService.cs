using System.Globalization;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;

namespace SkillHub.Infrastructure.Services;

public sealed class CertificatePdfService : ICertificatePdfService
{
    public byte[] GenerateEnrollmentCertificate(CertificateDataDto data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var memoryStream = new MemoryStream();
        using (var writer = new PdfWriter(memoryStream))
        using (var pdf = new PdfDocument(writer))
        using (var document = new Document(pdf, PageSize.LETTER))
        {
            document.SetMargins(42, 48, 42, 48);

            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontOblique = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            var primaryColor = new DeviceRgb(15, 23, 42);      // Deep Slate
            var accentColor = new DeviceRgb(37, 99, 235);      // Royal Blue
            var mutedText = new DeviceRgb(100, 116, 139);      // Slate Gray
            var cardBackground = new DeviceRgb(248, 250, 252); // Soft Surface
            var borderColor = new DeviceRgb(226, 232, 240);    // Subtle Border

            document.SetFont(fontRegular);

            // Header Banner
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth()
                .SetBackgroundColor(primaryColor)
                .SetPadding(18);

            var brandCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("SkillHub")
                    .SetFont(fontBold)
                    .SetFontSize(24)
                    .SetFontColor(ColorConstants.WHITE)
                    .SetMarginBottom(2))
                .Add(new Paragraph("In-Person Training & Practical Workshops Center")
                    .SetFont(fontRegular)
                    .SetFontSize(10)
                    .SetFontColor(new DeviceRgb(203, 213, 225)));

            var badgeCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("ACTIVE ENROLLMENT")
                    .SetFont(fontBold)
                    .SetFontSize(9)
                    .SetFontColor(new DeviceRgb(147, 197, 253)))
                .Add(new Paragraph($"Ref: {data.EnrollmentId.ToString()[..8].ToUpperInvariant()}")
                    .SetFont(fontRegular)
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.WHITE));

            headerTable.AddCell(brandCell);
            headerTable.AddCell(badgeCell);
            document.Add(headerTable);

            // Main Title (includes mandatory literal "Constancia de inscripción")
            document.Add(new Paragraph("Constancia de inscripción")
                .SetFont(fontBold)
                .SetFontSize(20)
                .SetFontColor(primaryColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(24)
                .SetMarginBottom(2));

            document.Add(new Paragraph("Official Workshop Enrollment Certificate")
                .SetFont(fontOblique)
                .SetFontSize(11)
                .SetFontColor(accentColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            document.Add(new Paragraph(
                    "SkillHub hereby certifies that the participant identified below holds an active, confirmed enrollment in the following scheduled in-person workshop:")
                .SetFontSize(11)
                .SetFontColor(primaryColor)
                .SetMarginBottom(18));

            // Participant Section
            document.Add(BuildSectionHeading("Participant Information", fontBold, accentColor));
            var participantTable = CreateDetailsTable(borderColor, cardBackground);
            AddDetailRow(participantTable, "Participant Full Name", data.ParticipantFullName, fontBold, fontRegular, borderColor);
            AddDetailRow(participantTable, "Participant Email", data.ParticipantEmail, fontBold, fontRegular, borderColor);
            document.Add(participantTable);

            // Workshop Section
            document.Add(BuildSectionHeading("Workshop Details", fontBold, accentColor).SetMarginTop(16));
            var workshopTable = CreateDetailsTable(borderColor, cardBackground);
            AddDetailRow(workshopTable, "Workshop Code", data.WorkshopCode, fontBold, fontRegular, borderColor);
            AddDetailRow(workshopTable, "Workshop Title", data.WorkshopTitle, fontBold, fontRegular, borderColor);
            AddDetailRow(workshopTable, "Instructor", data.InstructorFullName, fontBold, fontRegular, borderColor);

            var workshopDateFormatted = data.WorkshopStartDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var scheduleFormatted =
                $"{data.WorkshopStartDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} - {data.WorkshopEndDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC";

            AddDetailRow(workshopTable, "Workshop Date", workshopDateFormatted, fontBold, fontRegular, borderColor);
            AddDetailRow(workshopTable, "Workshop Schedule", scheduleFormatted, fontBold, fontRegular, borderColor);
            document.Add(workshopTable);

            // Audit & Timestamps Section
            document.Add(BuildSectionHeading("Registration & Issuance Metadata", fontBold, accentColor).SetMarginTop(16));
            var metadataTable = CreateDetailsTable(borderColor, cardBackground);
            AddDetailRow(
                metadataTable,
                "Enrollment Date",
                data.EnrolledAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                fontBold,
                fontRegular,
                borderColor);
            AddDetailRow(
                metadataTable,
                "Document Generation Date",
                data.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                fontBold,
                fontRegular,
                borderColor);
            AddDetailRow(
                metadataTable,
                "Enrollment Identifier",
                data.EnrollmentId.ToString(),
                fontBold,
                fontRegular,
                borderColor);
            document.Add(metadataTable);

            // Footer note
            document.Add(new Paragraph(
                    "This document was automatically generated by the SkillHub Academic Management Platform and is valid exclusively while the enrollment remains in Active status.")
                .SetFont(fontOblique)
                .SetFontSize(9)
                .SetFontColor(mutedText)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(28));
        }

        return memoryStream.ToArray();
    }

    private static Paragraph BuildSectionHeading(string text, PdfFont boldFont, Color accentColor) =>
        new Paragraph(text)
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetFontColor(accentColor)
            .SetMarginBottom(6);

    private static Table CreateDetailsTable(Color borderColor, Color backgroundColor) =>
        new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 }))
            .UseAllAvailableWidth()
            .SetBackgroundColor(backgroundColor)
            .SetBorder(new SolidBorder(borderColor, 1));

    private static void AddDetailRow(
        Table table,
        string label,
        string value,
        PdfFont boldFont,
        PdfFont regularFont,
        Color borderColor)
    {
        table.AddCell(new Cell()
            .SetBorder(new SolidBorder(borderColor, 0.5f))
            .SetPadding(8)
            .Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10)));

        table.AddCell(new Cell()
            .SetBorder(new SolidBorder(borderColor, 0.5f))
            .SetPadding(8)
            .Add(new Paragraph(value).SetFont(regularFont).SetFontSize(10)));
    }
}
