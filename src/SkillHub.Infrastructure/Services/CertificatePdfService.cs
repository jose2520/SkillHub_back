using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using iText.Barcodes;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;

namespace SkillHub.Infrastructure.Services;

/// <summary>
/// Generates authentic institutional Enrollment Certificates ("Constancia de inscripción")
/// following official academic letterhead and certification standards.
/// </summary>
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
            // Standard academic letter margins
            document.SetMargins(22, 38, 18, 38);

            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontOblique = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            var fontCourier = PdfFontFactory.CreateFont(StandardFonts.COURIER);

            document.SetFont(fontRegular);

            // Official SkillHub Palette
            var brandNavy = new DeviceRgb(11, 16, 33);         // #0B1021 Midnight Academic Navy
            var brandViolet = new DeviceRgb(109, 40, 217);     // #6D28D9 SkillHub Electric Violet
            var slateDark = new DeviceRgb(30, 41, 59);         // #1E293B Body Charcoal
            var slateMuted = new DeviceRgb(100, 116, 139);     // #64748B Secondary Text
            var cardSurface = new DeviceRgb(248, 250, 252);    // #F8FAFC Clean Card Surface
            var cardBorder = new DeviceRgb(226, 232, 240);     // #E2E8F0 Subtle Border
            var badgeGreen = new DeviceRgb(22, 101, 52);       // #166534 Confirmed Seat Green

            var refCode = $"SH-{data.EnrollmentId.ToString("N")[..8].ToUpperInvariant()}";
            var scheduleFormatted =
                $"{data.WorkshopStartDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} - {data.WorkshopEndDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC";
            var enrolledFormatted = data.EnrolledAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            var generatedFormatted = data.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

            // ---------------------------------------------------------------------------------
            // 1. TOP INSTITUTIONAL ACCENT STRIPE
            // ---------------------------------------------------------------------------------
            var topStripe = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(8);
            topStripe.AddCell(new Cell().SetHeight(3f).SetBackgroundColor(brandNavy).SetBorder(Border.NO_BORDER));
            topStripe.AddCell(new Cell().SetHeight(3f).SetBackgroundColor(brandViolet).SetBorder(Border.NO_BORDER));
            document.Add(topStripe);

            // ---------------------------------------------------------------------------------
            // 2. OFFICIAL LETTERHEAD: LOGO & INSTITUTIONAL CONTACT INFO
            // ---------------------------------------------------------------------------------
            var letterheadTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(8);

            var leftBrandCol = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE);
            var logoPath = BrandAssetResolver.ResolveAssetPath(BrandAssetResolver.HorizontalLogoFileName);
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                var logoData = ImageDataFactory.Create(logoPath);
                var logoImage = new Image(logoData)
                    .SetMaxWidth(148)
                    .SetMaxHeight(40)
                    .SetAutoScale(true)
                    .SetMarginBottom(2);
                leftBrandCol.Add(logoImage);
            }
            leftBrandCol.Add(new Paragraph("Centro de Formación Práctica y Desarrollo de Habilidades")
                .SetFont(fontBold)
                .SetFontSize(7.5f)
                .SetFontColor(brandNavy)
                .SetMargin(0));
            letterheadTable.AddCell(leftBrandCol);

            var rightMetaCol = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);
            rightMetaCol.Add(new Paragraph("DIRECCIÓN DE ADMISIONES Y REGISTRO ACADÉMICO")
                .SetFont(fontBold)
                .SetFontSize(7.3f)
                .SetFontColor(brandNavy)
                .SetMargin(0));
            rightMetaCol.Add(new Paragraph("Medellín, Colombia  •  contacto@skillhub.co\nSistema de Control de Cupos en Tiempo Real")
                .SetFontSize(7f)
                .SetFontColor(slateMuted)
                .SetMultipliedLeading(1.2f)
                .SetMarginTop(1)
                .SetMarginBottom(2));
            rightMetaCol.Add(new Paragraph($"Radicado No.: {refCode}")
                .SetFont(fontCourier)
                .SetFontSize(7.8f)
                .SetFontColor(brandViolet)
                .SetMargin(0));
            letterheadTable.AddCell(rightMetaCol);
            document.Add(letterheadTable);

            // Horizontal hairline divider
            var divider = new Table(1).UseAllAvailableWidth().SetMarginBottom(10);
            divider.AddCell(new Cell().SetHeight(0.8f).SetBackgroundColor(cardBorder).SetBorder(Border.NO_BORDER));
            document.Add(divider);

            // ---------------------------------------------------------------------------------
            // 3. CERTIFICATE TITLE
            // ---------------------------------------------------------------------------------
            document.Add(new Paragraph("Constancia de inscripción")
                .SetFont(fontBold)
                .SetFontSize(18f)
                .SetFontColor(brandNavy)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMargin(0));

            document.Add(new Paragraph("CERTIFICADO OFICIAL DE MATRÍCULA Y CUPO CONFIRMADO")
                .SetFont(fontBold)
                .SetFontSize(8.2f)
                .SetFontColor(brandViolet)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(2)
                .SetMarginBottom(10));

            // ---------------------------------------------------------------------------------
            // 4. ATTESTATION PREAMBLE & "HACE CONSTAR"
            // ---------------------------------------------------------------------------------
            document.Add(new Paragraph(
                    "El Centro de Capacitación Práctica SkillHub, a través de su Dirección de Registro y Control Académico,")
                .SetFont(fontRegular)
                .SetFontSize(9.2f)
                .SetFontColor(slateDark)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetMarginBottom(6));

            document.Add(new Paragraph("HACE CONSTAR:")
                .SetFont(fontBold)
                .SetFontSize(11f)
                .SetFontColor(brandNavy)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(1)
                .SetMarginBottom(6));

            // ---------------------------------------------------------------------------------
            // 5. PARTICIPANT CERTIFICATION STATEMENT
            // ---------------------------------------------------------------------------------
            var partStatement = new Paragraph()
                .SetFontSize(9.2f)
                .SetFontColor(slateDark)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetMultipliedLeading(1.3f)
                .SetMarginBottom(10);

            partStatement.Add("Que el/la participante ");
            partStatement.Add(new Text(data.ParticipantFullName).SetFont(fontBold).SetFontColor(brandNavy));
            partStatement.Add(", identificado(a) en el sistema institucional con el correo electrónico ");
            partStatement.Add(new Text(data.ParticipantEmail).SetFont(fontBold).SetFontColor(brandNavy));
            partStatement.Add(", se encuentra debidamente ");
            partStatement.Add(new Text("INSCRITO(A)").SetFont(fontBold).SetFontColor(brandViolet));
            partStatement.Add(" y con cupo activo confirmado para asistir al taller práctico presencial detallado a continuación:");
            document.Add(partStatement);

            // ---------------------------------------------------------------------------------
            // 6. TECHNICAL SUMMARY CARD (FICHA TÉCNICA DEL TALLER)
            // ---------------------------------------------------------------------------------
            var summaryCard = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 }))
                .UseAllAvailableWidth()
                .SetBackgroundColor(cardSurface)
                .SetBorder(new SolidBorder(cardBorder, 0.8f))
                .SetBorderLeft(new SolidBorder(brandViolet, 3.5f))
                .SetMarginBottom(10);

            AddSummaryRow(summaryCard, "Nombre del Taller:", data.WorkshopTitle, fontBold, fontBold, brandNavy, brandNavy);
            AddSummaryRow(summaryCard, "Código de Referencia:", data.WorkshopCode, fontRegular, fontBold, slateMuted, brandViolet);
            AddSummaryRow(summaryCard, "Instructor a Cargo:", data.InstructorFullName, fontRegular, fontBold, slateMuted, slateDark);
            AddSummaryRow(summaryCard, "Fecha y Horario (UTC):", scheduleFormatted, fontRegular, fontBold, slateMuted, slateDark);
            AddSummaryRow(summaryCard, "Modalidad y Sede:", "Presencial  •  Laboratorios Prácticos SkillHub", fontRegular, fontRegular, slateMuted, slateDark);
            AddSummaryRow(summaryCard, "Estado de la Matrícula:", "● ACTIVA (Cupo confirmado y garantizado)", fontRegular, fontBold, slateMuted, badgeGreen);

            document.Add(summaryCard);

            // ---------------------------------------------------------------------------------
            // 7. FORMAL CLOSING & AUDIT TIMESTAMPS
            // ---------------------------------------------------------------------------------
            var closingText = new Paragraph(
                    "La presente constancia se expide a solicitud de la persona interesada para los fines pertinentes, en constancia de su matrícula formal y reserva de cupo en la plataforma SkillHub.")
                .SetFont(fontRegular)
                .SetFontSize(8.8f)
                .SetFontColor(slateDark)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetMultipliedLeading(1.25f)
                .SetMarginBottom(8);
            document.Add(closingText);

            var auditTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER)
                .SetMarginBottom(12);

            var auditLeft = new Cell().SetBorder(Border.NO_BORDER);
            auditLeft.Add(new Paragraph($"Fecha de inscripción: {enrolledFormatted}")
                .SetFontSize(7.6f)
                .SetFontColor(slateMuted)
                .SetMargin(0));
            auditTable.AddCell(auditLeft);

            var auditRight = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
            auditRight.Add(new Paragraph($"Fecha de expedición: {generatedFormatted}")
                .SetFontSize(7.6f)
                .SetFontColor(slateMuted)
                .SetMargin(0));
            auditTable.AddCell(auditRight);
            document.Add(auditTable);

            // ---------------------------------------------------------------------------------
            // 8. SIGNATURES & VERIFICATION AREA
            // ---------------------------------------------------------------------------------
            var verificationHash = ComputeVerificationHash(data);
            var qrPayload =
                $"SkillHub|Constancia|{data.EnrollmentId}|{data.WorkshopCode}|{data.ParticipantEmail}|{data.EnrolledAtUtc:O}|SHA256:{verificationHash[..24]}";

            var bottomSection = new Table(UnitValue.CreatePercentArray(new float[] { 22, 38, 40 }))
                .UseAllAvailableWidth()
                .SetBorder(Border.NO_BORDER);

            // Left: Verification QR
            var qrCol = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM);
            try
            {
                var qrCode = new BarcodeQRCode(qrPayload);
                var qrXObject = qrCode.CreateFormXObject(brandNavy, pdf);
                var qrImage = new Image(qrXObject).SetWidth(50).SetHeight(50);
                qrCol.Add(qrImage);
            }
            catch
            {
                qrCol.Add(new Paragraph("[QR]").SetFont(fontBold).SetFontSize(8f));
            }

            qrCol.Add(new Paragraph("VALIDACIÓN DIGITAL")
                .SetFont(fontBold)
                .SetFontSize(6.2f)
                .SetFontColor(brandNavy)
                .SetMarginTop(2)
                .SetMarginBottom(0));
            qrCol.Add(new Paragraph($"SHA-256: {verificationHash[..14]}...")
                .SetFont(fontCourier)
                .SetFontSize(5.6f)
                .SetFontColor(brandViolet)
                .SetMargin(0));
            bottomSection.AddCell(qrCol);

            // Center: Institutional Emblem Seal
            var sealCol = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.BOTTOM)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetPaddingLeft(6);

            var emblemPath = BrandAssetResolver.ResolveAssetPath(BrandAssetResolver.EmblemFileName);
            if (!string.IsNullOrEmpty(emblemPath) && File.Exists(emblemPath))
            {
                var emblemData = ImageDataFactory.Create(emblemPath);
                var emblemImg = new Image(emblemData)
                    .SetMaxWidth(34)
                    .SetMaxHeight(26)
                    .SetAutoScale(true)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                    .SetMarginBottom(2);
                sealCol.Add(emblemImg);
            }

            sealCol.Add(new Paragraph("SELLO INSTITUCIONAL\nSkillHub Academic Center")
                .SetFont(fontBold)
                .SetFontSize(6.2f)
                .SetFontColor(brandNavy)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMultipliedLeading(1.15f)
                .SetMargin(0));
            bottomSection.AddCell(sealCol);

            // Right: Formal Institutional Signatures
            var sigCol = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.BOTTOM)
                .SetPaddingLeft(10);

            var sigTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();

            sigTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPaddingRight(4)
                .Add(new Paragraph("_______________________\nCOORDINACIÓN ACADÉMICA\nSkillHub Training Center")
                    .SetFont(fontBold)
                    .SetFontSize(6.4f)
                    .SetFontColor(slateDark)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMultipliedLeading(1.2f)));

            sigTable.AddCell(new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPaddingLeft(4)
                .Add(new Paragraph($"_______________________\nINSTRUCTOR PRINCIPAL\n{data.InstructorFullName}")
                    .SetFont(fontBold)
                    .SetFontSize(6.4f)
                    .SetFontColor(slateDark)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMultipliedLeading(1.2f)));

            sigCol.Add(sigTable);
            bottomSection.AddCell(sigCol);
            document.Add(bottomSection);

            // ---------------------------------------------------------------------------------
            // 9. FOOTER
            // ---------------------------------------------------------------------------------
            var footerDivider = new Table(1).UseAllAvailableWidth().SetMarginTop(8);
            footerDivider.AddCell(new Cell().SetHeight(0.6f).SetBackgroundColor(cardBorder).SetBorder(Border.NO_BORDER));
            document.Add(footerDivider);

            document.Add(new Paragraph(
                    "SkillHub  •  Aprende • Crea • Crece  •  Documento Oficial de Registro Académico  •  Página 1 de 1")
                .SetFont(fontOblique)
                .SetFontSize(6.6f)
                .SetFontColor(slateMuted)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(3)
                .SetMarginBottom(0));

            // Apply watermark (logo1.png centered in background at 6% opacity)
            ApplyWatermark(pdf);
        }

        return memoryStream.ToArray();
    }

    private static void AddSummaryRow(
        Table table,
        string label,
        string value,
        PdfFont labelFont,
        PdfFont valueFont,
        Color labelColor,
        Color valueColor)
    {
        table.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(241, 245, 249), 0.6f))
            .SetPadding(5)
            .Add(new Paragraph(label)
                .SetFont(labelFont)
                .SetFontSize(8f)
                .SetFontColor(labelColor)
                .SetMargin(0)));

        table.AddCell(new Cell()
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(new DeviceRgb(241, 245, 249), 0.6f))
            .SetPadding(5)
            .Add(new Paragraph(value)
                .SetFont(valueFont)
                .SetFontSize(8.4f)
                .SetFontColor(valueColor)
                .SetMargin(0)));
    }

    private static string ComputeVerificationHash(CertificateDataDto data)
    {
        var payload =
            $"{data.EnrollmentId:D}|{data.ParticipantEmail}|{data.WorkshopCode}|{data.WorkshopStartDateTime:O}|{data.EnrolledAtUtc:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void ApplyWatermark(PdfDocument pdf)
    {
        var watermarkPath = BrandAssetResolver.ResolveAssetPath(BrandAssetResolver.WatermarkLogoFileName)
            ?? BrandAssetResolver.ResolveAssetPath(BrandAssetResolver.EmblemFileName);

        if (string.IsNullOrEmpty(watermarkPath) || !File.Exists(watermarkPath))
        {
            return;
        }

        var watermarkData = ImageDataFactory.Create(watermarkPath);
        const float targetWidth = 330f;
        var targetHeight = targetWidth * (watermarkData.GetHeight() / watermarkData.GetWidth());

        for (var pageNum = 1; pageNum <= pdf.GetNumberOfPages(); pageNum++)
        {
            var page = pdf.GetPage(pageNum);
            var pageSize = page.GetPageSize();

            var x = (pageSize.GetWidth() - targetWidth) / 2f;
            var y = (pageSize.GetHeight() - targetHeight) / 2f - 10f;

            var underCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdf);
            using var canvas = new Canvas(underCanvas, pageSize);

            var watermarkImg = new Image(watermarkData)
                .SetWidth(targetWidth)
                .SetHeight(targetHeight)
                .SetFixedPosition(x, y, targetWidth)
                .SetOpacity(0.06f);

            canvas.Add(watermarkImg);
        }
    }
}
