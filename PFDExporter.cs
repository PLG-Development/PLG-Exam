using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;

using System.Globalization;
using MigraDoc.DocumentObjectModel;
using System.Net.NetworkInformation;
using Avalonia.Styling;

namespace PLG_Exam;

public static class PFDExporter
{
    public static Exam exam;
    public static async void ExportToPdf(Exam examData)
    {
        try
        {
            exam = examData;
            //_ = GetCurrentExamDataAsJson();

            if(examData.Name.IsValueNullOrEmpty() || examData.Vorname.IsValueNullOrEmpty() || examData.Title.IsValueNullOrEmpty() || examData.Datum == null){
                await MessageBox.Show(MainWindow._instance, "Bitte füllen Sie mindestens die Felder für Name, Vorname, Titel und Datum aus", "Fehler", MessageBoxButton.Ok);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                DefaultExtension = "pdf",
                Filters = { new FileDialogFilter { Name = "PDF files", Extensions = { "pdf" } } }
            };
            var filePath = await saveDialog.ShowAsync(MainWindow._instance);
            if (string.IsNullOrEmpty(filePath)) return;

            var font = new XFont("Cantarell", 14, XFontStyleEx.Bold);
            var fontB = new XFont("Cantarell", 14, XFontStyleEx.Bold);
            var fontsmall = new XFont("Cantarell", 10, XFontStyleEx.Regular);
            var descriptionFont = new XFont("Cantarell", 12, XFontStyleEx.Regular);

            using var document = new PdfDocument();

            document.Info.Title = examData.Title;
            document.Info.Author = examData.Vorname + " " + examData.Name;
            document.Info.Subject = "PLG Exam Submission";
            document.Info.Keywords = "Exam, PLG, Report, PDF";
            SetPdfLanguage(document);


            var firstPage = document.AddPage();
            var gfx = XGraphics.FromPdfPage(firstPage);
            gfx.DrawString(examData.Title, fontB, XBrushes.Black, new XRect(0, 40, firstPage.Width, 50), XStringFormats.TopCenter);

            gfx.DrawString($"Name: {examData.Name}", fontB, XBrushes.Black, new XRect(50, 100, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);
            gfx.DrawString($"Vorname: {examData.Vorname}", fontB, XBrushes.Black, new XRect(50, 130, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);
            gfx.DrawString($"Datum: {examData.Datum?.ToShortDateString() ?? "N/A"}", fontB, XBrushes.Black, new XRect(50, 160, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);

            foreach (var tab in examData.Tabs)
            {
                DrawTaskWithPageBreak(document, tab, descriptionFont, font, fontsmall);
            }


            var endpage = document.AddPage();
            var endpageGfx = XGraphics.FromPdfPage(endpage);

            // Beschreibung am unteren Rand platzieren
            var bottomDescriptionRect = new XRect(
                50,                                  // X-Position (horizontaler Abstand)
                endpage.Height - 100,                // Y-Position (Seitenhöhe - Textbereichshöhe - Margin)
                endpage.Width - 100,                 // Breite des Bereichs
                100                                  // Höhe des Bereichs
            );

            var endformatter = new XTextFormatter(endpageGfx)
            {
                Alignment = XParagraphAlignment.Left
            };

            // Text zeichnen
            endformatter.DrawString(
                "Erstellt mit PLG Exam - powered by PLG Development\nhttps://github.com/PLG-Development/PLG-Exam\n(c) 2025 - PLG Development",
                fontsmall,
                XBrushes.Black,
                bottomDescriptionRect
            );        

                // Beschreibung am unteren Rand platzieren
            var informationRect = new XRect(
                50,                                  // X-Position (horizontaler Abstand)
                50,                // Y-Position (Seitenhöhe - Textbereichshöhe - Margin)
                endpage.Width - 100,                 // Breite des Bereichs
                300                                  // Höhe des Bereichs
            );

            string outinfo = $"Dieses Dokument wurde automatisch erstellt und enthält alle Bestandteile Ihrer Prüfung. Bitte prüfen Sie alle Inhalte vor der Abgabe und speichern das Dokument an einem sicheren Ort. Nutzen Sie zur Abgabe bitte einen durch die Lehrkraft gestellten USB-Stick.";
            outinfo += $"\n\nErstelldatum: {DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.CreateSpecificCulture("de-DE"))}";
            outinfo += $"\nErstellzeit: {DateTime.Now.ToString("HH:mm:ss", CultureInfo.CreateSpecificCulture("de-DE"))} Uhr";
            outinfo += $"\nGerätename: {Environment.MachineName}";
            outinfo += $"\nBenutzername: {Environment.UserName}";
            outinfo += $"\nBetriebssystem: {Environment.OSVersion.VersionString}";
            outinfo += $"\nIP-Adresse: {System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)}";
            outinfo += $"\nInternetverbindung: {(isInternetAvailable() ? "8.8.8.8 ist erreichbar" : "8.8.8.8 ist nicht erreichbar")}";

            endformatter.DrawString(
                outinfo,
                fontsmall,
                XBrushes.Black,
                informationRect
            );

            var page = $"Seite {document.PageCount}";
            endpageGfx.DrawString(page, fontsmall, XBrushes.Gray, new XRect(50, gfx.PageSize.Height - 50 + 15, gfx.PageSize.Width - 50 * 2, 50), XStringFormats.TopRight);


            endpageGfx.Dispose();
            gfx.Dispose();
            //AddPageNumbers(document, fontsmall, 35);
            document.Save(filePath);
            await MessageBox.Show(MainWindow._instance, "PDF erfolgreich gespeichert!", "Erfolg", MessageBoxButton.Ok);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim PDF-Export: {ex.Message}");
            await MessageBox.Show(MainWindow._instance, "Fehler beim PDF-Export.", "Fehler", MessageBoxButton.Ok);
        }
    }

    private static bool isInternetAvailable()
    {
        try
        {
            using (var ping = new Ping())
            {
                var reply = ping.Send("8.8.8.8", 2000); // 3000 ms timeout
                return reply.Status == IPStatus.Success;
            }
        }
        catch
        {
            return false;
        }
    }
    public static bool isEscaped = false;
    public static bool isBold = false;
    public static bool isUnderline = false;
    private static List<(string Text, XFont Font, XBrush Brush)> ParseFormattedText(string text, XFont regularFont, XFont boldFont, XFont underlineFont)
    {
        var result = new List<(string Text, XFont Font, XBrush Brush)>();
        var currentFont = regularFont;
        var currentBrush = XBrushes.Black;

        var buffer = new System.Text.StringBuilder();
        

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (isEscaped)
            {
                buffer.Append(c);
                isEscaped = false;
                continue;
            }

            if (c == '\\')
            {
                isEscaped = true;
                continue;
            }

            if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                if (isBold)
                {
                    result.Add((buffer.ToString(), currentFont, currentBrush));
                    buffer.Clear();
                    currentFont = isUnderline ? underlineFont : regularFont;
                    isBold = false;
                }
                else
                {
                    result.Add((buffer.ToString(), currentFont, currentBrush));
                    buffer.Clear();
                    currentFont = boldFont;
                    isBold = true;
                }
                i++; // Skip the second '*'
                continue;
            }

            if (c == '_')
            {
                if (isUnderline)
                {
                    result.Add((buffer.ToString(), currentFont, currentBrush));
                    buffer.Clear();
                    currentFont = isBold ? boldFont : regularFont;
                    isUnderline = false;
                }
                else
                {
                    result.Add((buffer.ToString(), currentFont, currentBrush));
                    buffer.Clear();
                    currentFont = underlineFont;
                    isUnderline = true;
                }
                continue;
            }

            if(isUnderline){
                currentFont = underlineFont;
            } else if (isBold){
                currentFont = boldFont;
            } else {
                currentFont = regularFont;
            }

            buffer.Append(c);
        }

        if (buffer.Length > 0)
        {
            result.Add((buffer.ToString(), currentFont, currentBrush));
        }

        return result;
    }

    private static void DrawFormattedText(XGraphics gfx, string text, XFont regularFont, XFont boldFont, XFont underlineFont, XRect rect)
    {
        var parsedText = ParseFormattedText(text, regularFont, boldFont, underlineFont);
        double x = rect.X;
        double y = rect.Y;

        foreach (var (Text, Font, Brush) in parsedText)
        {
            var size = gfx.MeasureString(Text, Font);
            gfx.DrawString(Text, Font, Brush, new XRect(x, y, rect.Width, rect.Height), XStringFormats.TopLeft);
            x += size.Width;
        }
    }

    private static void DrawTaskWithPageBreak(PdfDocument document, ExamTab tab, XFont font, XFont headerFont, XFont smallFont)
    {
        const double margin = 50; // Seitenränder
        const double corr_margin = 300; // Seitenrand Korrektur
        const double lineHeight = 21; // Höhe einer Textzeile
        const double headerHeight = 30; // Platz für die Kopfzeile pro Seite
        const double footerHeight = 20; // Platz für die Fußzeile
        const double usableHeight = 842 - margin * 2 - headerHeight - footerHeight; // Höhe des nutzbaren Bereichs

        var lines = SplitTextIntoLines(tab.Inhalt, document.Pages[0].Width - margin - corr_margin, font);

        double currentHeight = 0;
        PdfPage page = null;
        XGraphics gfx = null;

        foreach (var line in lines)
        {
            if (page == null || currentHeight + lineHeight > usableHeight)
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                currentHeight = 0;

                DrawName(gfx, tab, smallFont, margin, headerHeight, document.PageCount);
                DrawHeader(gfx, tab, headerFont, margin, headerHeight);
            }

            var rect = new XRect(corr_margin, margin + headerHeight * headerline_count + currentHeight, page.Width - margin, lineHeight);
            DrawFormattedText(gfx, line, font, new XFont(font.Name, font.Size, XFontStyleEx.Bold), new XFont(font.Name, font.Size, XFontStyleEx.Underline), rect);
            currentHeight += lineHeight;
        }

        gfx?.Dispose();
    }

    private static int headerline_count = 0;
    private static void DrawHeader(XGraphics gfx, ExamTab tab, XFont font, double margin, double headerHeight)
    {
        var maxWidth = gfx.PageSize.Width - margin * 2; // verfügbare Breite
        var lines = SplitTextIntoLines($"Aufgabe {tab.Aufgabennummer}: {tab.Überschrift}", maxWidth, font);

        double currentY = margin;
        headerline_count=0;
        foreach (var line in lines)
        {
            gfx.DrawString(line, font, XBrushes.Gray, new XRect(margin, currentY, maxWidth, headerHeight), XStringFormats.TopLeft);
            currentY += font.Height; 

            headerline_count++;
        }

        
    }

    private static void AddPageNumbers(PdfDocument document, XFont font, double margin)
    {
        
        int totalPages = document.PageCount;
        for (int i = 1; i < totalPages; i++) // Beginnt ab der zweiten Seite (Index 1)
        {
            PdfPage page = document.Pages[i];
            XGraphics gfx = XGraphics.FromPdfPage(page);
            string text = $"Seite {i + 1} von {totalPages}";
            double yPosition = page.Height - margin;
            gfx.DrawString(text, font, XBrushes.Gray, new XRect(margin, yPosition, page.Width - 2 * margin, 20), XStringFormats.Center);
            gfx.Dispose();
            
        }
    }

    private static void DrawName(XGraphics gfx, ExamTab tab, XFont font, double margin, double headerHeight, int page_num)
    {
        var headerText = $"{exam.Name}, {exam.Vorname}";
        gfx.DrawString(headerText, font, XBrushes.Gray, new XRect(margin, margin-15, gfx.PageSize.Width - margin * 2, headerHeight), XStringFormats.TopLeft);

        var headerText2 = exam.Datum.Value.ToString("dd.MM.yyyy", CultureInfo.CreateSpecificCulture("de-DE"));
        gfx.DrawString(headerText2, font, XBrushes.Gray, new XRect(margin, margin-15, gfx.PageSize.Width - margin * 2, headerHeight), XStringFormats.TopRight);
        
        var page = $"Seite {page_num}";
        gfx.DrawString(page, font, XBrushes.Gray, new XRect(margin, gfx.PageSize.Height - margin + 15, gfx.PageSize.Width - margin * 2, headerHeight), XStringFormats.TopRight);

    }

    // Methode zum Aufteilen des Textes in Zeilen
    private static List<string> SplitTextIntoLines(string text, double maxWidth, XFont font)
    {
        var lines = new List<string>();

        // Text anhand von \n aufteilen, einschließlich leerer Zeilen
        var manualLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        using (var gfx = XGraphics.CreateMeasureContext(new XSize(maxWidth, 842), XGraphicsUnit.Point, XPageDirection.Downwards))
        {
            foreach (var manualLine in manualLines)
            {
                // Falls die Zeile leer ist (z. B. durch mehrere \n), direkt hinzufügen
                if (string.IsNullOrWhiteSpace(manualLine))
                {
                    lines.Add(""); // Leere Zeile hinzufügen
                    continue;
                }

                // Falls eine Zeile zu lang ist, weiter aufteilen
                var words = manualLine.Split(' ');
                var currentLine = "";

                foreach (var word in words)
                {
                    var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                    var size = gfx.MeasureString(testLine, font);

                    if (size.Width > maxWidth)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                // Letzte Zeile hinzufügen
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }

                
            }
            gfx.Dispose();
        }

        return lines;
    }

    

    private static void SetPdfLanguage(PdfSharp.Pdf.PdfDocument document, string language = "de")
    {
        var catalog = document.Internals.Catalog;

        if (catalog.Elements.ContainsKey("/Lang"))
        {
            catalog.Elements["/Lang"] = new PdfSharp.Pdf.PdfString(language);
        }
        else
        {
            catalog.Elements.Add("/Lang", new PdfSharp.Pdf.PdfString(language));
        }
    }
}
