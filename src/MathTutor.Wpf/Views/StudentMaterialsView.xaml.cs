using System.Windows.Controls;
using System.Windows;

namespace MathTutor.Wpf.Views;

public partial class StudentMaterialsView : UserControl
{
    public StudentMaterialsView()
    {
        InitializeComponent();
    }

    private void OpenPracticeLatex_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LatexEditorWindow.InsertInto(PracticeAnswerTextBox);
    }

    private void OpenTestLatex_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        LatexEditorWindow.InsertInto(TestAnswerTextBox);
    }

    private void OpenSolver_Click(object sender, RoutedEventArgs e)
    {
        var window = new PracticeSolverWindow
        {
            Owner = Window.GetWindow(this),
            DataContext = DataContext
        };
        window.ShowDialog();
    }
}

