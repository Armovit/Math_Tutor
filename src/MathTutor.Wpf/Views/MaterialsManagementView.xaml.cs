using System.Windows.Controls;

namespace MathTutor.Wpf.Views;

public partial class MaterialsManagementView : UserControl
{
    public MaterialsManagementView()
    {
        InitializeComponent();
    }

    private void OpenTopicLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(TopicTextBox);

    private void OpenSectionLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(SectionTextBox);

    private void OpenTitleLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(TitleTextBox);

    private void OpenDescriptionLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(DescriptionTextBox);

    private void OpenTheoryLatex_Click(object sender, System.Windows.RoutedEventArgs e) => LatexEditorWindow.InsertInto(TheoryTextBox);
}

