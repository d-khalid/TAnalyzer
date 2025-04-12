using Avalonia.Controls;
using TAnalyzer.ViewModels;


namespace TAnalyzer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        
        // Set the data context for the XAML Bindings
        DataContext = new MainWindowViewModel();
    }
}