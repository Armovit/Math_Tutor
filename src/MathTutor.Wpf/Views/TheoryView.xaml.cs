using System.Windows.Controls;
using System.Windows;

namespace MathTutor.Wpf.Views;

public partial class TheoryView : UserControl
{
    public TheoryView()
    {
        InitializeComponent();
    }

    private void OpenReader_Click(object sender, RoutedEventArgs e)
    {
        var window = new TheoryReaderWindow
        {
            Owner = Window.GetWindow(this),
            DataContext = DataContext
        };
        window.ShowDialog();
    }
}
