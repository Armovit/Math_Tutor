using System.Windows;

namespace MathTutor.Wpf.Views;

public partial class PracticeSolverWindow : Window
{
    public PracticeSolverWindow()
    {
        InitializeComponent();
    }

    private void OpenPracticeLatex_Click(object sender, RoutedEventArgs e)
    {
        LatexEditorWindow.InsertInto(PracticeAnswerTextBox);
    }

    private void OpenTestLatex_Click(object sender, RoutedEventArgs e)
    {
        LatexEditorWindow.InsertInto(TestAnswerTextBox);
    }
}
