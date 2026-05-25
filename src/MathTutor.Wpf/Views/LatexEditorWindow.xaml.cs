using System.Windows;
using System.Windows.Controls;

namespace MathTutor.Wpf.Views;

public partial class LatexEditorWindow : Window
{
    public LatexEditorWindow(string initialFormula = "")
    {
        InitializeComponent();
        FormulaTextBox.Text = initialFormula;
        FormulaTextBox.CaretIndex = FormulaTextBox.Text.Length;
        FormulaTextBox.Focus();
    }

    public string FormulaText => FormulaTextBox.Text.Trim();

    public static void InsertInto(TextBox target)
    {
        var window = new LatexEditorWindow();
        window.Owner = Window.GetWindow(target);
        if (window.ShowDialog() != true || string.IsNullOrWhiteSpace(window.FormulaText)) return;

        var insertion = $"\\({window.FormulaText}\\)";
        var start = target.SelectionStart;
        var selectedLength = target.SelectionLength;
        var text = target.Text ?? string.Empty;

        target.Text = text.Remove(start, selectedLength).Insert(start, insertion);
        target.CaretIndex = start + insertion.Length;
        target.Focus();
        target.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void InsertTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string template }) return;
        var start = FormulaTextBox.SelectionStart;
        var selectedLength = FormulaTextBox.SelectionLength;
        var text = FormulaTextBox.Text ?? string.Empty;

        FormulaTextBox.Text = text.Remove(start, selectedLength).Insert(start, template);
        FormulaTextBox.CaretIndex = start + template.Length;
        FormulaTextBox.Focus();
    }

    private void Insert_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
