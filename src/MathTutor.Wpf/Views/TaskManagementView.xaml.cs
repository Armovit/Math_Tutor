using System.Windows.Controls;

namespace MathTutor.Wpf.Views;

public partial class TaskManagementView : UserControl
{
    public TaskManagementView()
    {
        InitializeComponent();
    }

    private void OpenTitleLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(TitleTextBox);

    private void OpenQuestionLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(QuestionTextBox);

    private void OpenOptionLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(OptionTextBox);

    private void OpenCorrectAnswerLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(CorrectAnswerTextBox);

    private void OpenExplanationLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(ExplanationTextBox);
}

