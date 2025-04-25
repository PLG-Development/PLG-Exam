using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PLG_Exam.ViewModels;

public class ExportOptionsViewModel : INotifyPropertyChanged
{
    private bool hasCorrectionLines = false;
    private int correctionMargin = 50;
    private double lineSpacing= 1.5;
    private string textPosition="left";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ToggleSwitch
    public bool HasCorrectionLines
    {
        get => hasCorrectionLines;
        set { hasCorrectionLines = value; OnPropertyChanged(); }
    }

    // Korrekturrand – RadioButtons mapped auf int
    public bool IsCorrectionMarginHalf
    {
        get => correctionMargin == 100;
        set { if (value) { correctionMargin = 100; OnPropertyChanged(); } }
    }

    public bool IsCorrectionMarginQuarter
    {
        get => correctionMargin == 50;
        set { if (value) { correctionMargin = 50; OnPropertyChanged(); } }
    }

    public bool IsCorrectionMarginNone
    {
        get => correctionMargin == 0;
        set { if (value) { correctionMargin = 0; OnPropertyChanged(); } }
    }

    // Zeilenabstand
    public bool IsLineSpacing1
    {
        get => lineSpacing == 1.0;
        set { if (value) { lineSpacing = 1.0; OnPropertyChanged(); } }
    }

    public bool IsLineSpacing15
    {
        get => lineSpacing == 1.5;
        set { if (value) { lineSpacing = 1.5; OnPropertyChanged(); } }
    }

    public bool IsLineSpacing2
    {
        get => lineSpacing == 2.0;
        set { if (value) { lineSpacing = 2.0; OnPropertyChanged(); } }
    }

    // Textposition
    public bool IsTextLeft
    {
        get => textPosition == "left";
        set { if (value) { textPosition = "left"; OnPropertyChanged(); } }
    }

    public bool IsTextRight
    {
        get => textPosition == "right";
        set { if (value) { textPosition = "right"; OnPropertyChanged(); } }
    }

    // Optional: Getter für externe Verwendung
    public int CorrectionMargin => correctionMargin;
    public double LineSpacing => lineSpacing;
    public string TextPosition => textPosition;
}