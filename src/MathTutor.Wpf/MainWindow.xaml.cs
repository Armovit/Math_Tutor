using System.Windows;
using MathTutor.Wpf.ViewModels;

namespace MathTutor.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
