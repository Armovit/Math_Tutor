using System.Windows;
using MathTutor.Application.DTOs;
using MathTutor.Wpf.ViewModels;

namespace MathTutor.Wpf.Views;

public partial class TestResultsWindow : Window
{
    public TestResultsWindow(TestAttemptDto attempt)
    {
        InitializeComponent();
        DataContext = new TestResultsWindowViewModel(attempt);
    }

    public static void ShowDialog(TestAttemptDto attempt, Window? owner = null)
    {
        var window = new TestResultsWindow(attempt)
        {
            Owner = owner ?? System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
