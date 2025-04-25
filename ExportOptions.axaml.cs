using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PLG_Exam.ViewModels;


namespace PLG_Exam;

public partial class ExportOptions : Window
{
    public ExportOptions()
    {
        InitializeComponent();
        DataContext = new ExportOptionsViewModel();
    }

    private async void OnSubmitClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (ExportOptionsViewModel)DataContext;
        MainWindow._instance.ExportToPdf(viewModel.CorrectionMargin, viewModel.LineSpacing, viewModel.HasCorrectionLines, viewModel.TextPosition);
        this.Close();
    }

    

}