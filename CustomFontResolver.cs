using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System.IO;
using System;

namespace PLG_Exam;

public class CustomFontResolver : IFontResolver
{
    private static readonly byte[] Cantarell_RegularFont;
    private static readonly byte[] Cantarell_BoldFont;

    static CustomFontResolver()
    {
        // Schriftarten aus Dateien laden
        var basePath = Path.Combine(AppContext.BaseDirectory, "resources");
        var regularPath = Path.Combine(basePath, "Cantarell-Regular.ttf");
        var boldPath = Path.Combine(basePath, "Cantarell-Bold.ttf");

        Cantarell_RegularFont = File.ReadAllBytes(regularPath);
        Cantarell_BoldFont = File.ReadAllBytes(boldPath);
    }

    public string DefaultFontName => "Cantarell";

    public byte[] GetFont(string faceName)
    {
        return faceName switch
        {
            "Cantarell-Regular" => Cantarell_RegularFont,
            "Cantarell-Bold" => Cantarell_BoldFont,
            _ => throw new FileNotFoundException($"Schriftart nicht gefunden: {faceName}")
        };
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName == "Cantarell")
        {
            // Bold oder Regular zurückgeben
            return isBold 
                ? new FontResolverInfo("Cantarell-Bold") 
                : new FontResolverInfo("Cantarell-Regular");
        }

        throw new FileNotFoundException($"Schriftart nicht gefunden: {familyName}");
    }
}
