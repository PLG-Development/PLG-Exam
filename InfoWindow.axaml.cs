using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PLG_Exam;

public partial class InfoWindow : Window
{
    public InfoWindow(string cnt)
    {
        InitializeComponent();

        LblInfo.Content=cnt;
    }
}