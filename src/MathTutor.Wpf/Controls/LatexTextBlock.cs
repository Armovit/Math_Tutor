using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WpfMath.Controls;

namespace MathTutor.Wpf.Controls;

public sealed partial class LatexTextBlock : FlowDocumentScrollViewer
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(LatexTextBlock), new PropertyMetadata(string.Empty, OnLayoutPropertyChanged));

    public static readonly DependencyProperty FormulaFontSizeProperty =
        DependencyProperty.Register(nameof(FormulaFontSize), typeof(double), typeof(LatexTextBlock), new PropertyMetadata(20d, OnLayoutPropertyChanged));

    public static readonly DependencyProperty TextLineHeightProperty =
        DependencyProperty.Register(nameof(TextLineHeight), typeof(double), typeof(LatexTextBlock), new PropertyMetadata(24d, OnLayoutPropertyChanged));

    private static readonly Regex LatexTokenRegex = LatexRegex();
    private static readonly Regex SqrtCallRegex = SqrtRegex();
    private static readonly Regex PowerRegex = PowerExpressionRegex();
    private static readonly Regex EquationRegex = EquationExpressionRegex();
    private static readonly Regex StandaloneMathRegex = StandaloneExpressionRegex();

    public LatexTextBlock()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Focusable = false;
        IsSelectionEnabled = false;
        UpdateDocument();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double FormulaFontSize
    {
        get => (double)GetValue(FormulaFontSizeProperty);
        set => SetValue(FormulaFontSizeProperty, value);
    }

    public double TextLineHeight
    {
        get => (double)GetValue(TextLineHeightProperty);
        set => SetValue(TextLineHeightProperty, value);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ForegroundProperty || e.Property == FontSizeProperty || e.Property == FontFamilyProperty)
        {
            UpdateDocument();
        }
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((LatexTextBlock)d).UpdateDocument();
    }

    private void UpdateDocument()
    {
        var document = new FlowDocument
        {
            Background = Brushes.Transparent,
            PagePadding = new Thickness(0),
            FontFamily = FontFamily,
            FontSize = FontSize,
            Foreground = Foreground
        };

        var source = AutoFormatPlainMath(Text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(source))
        {
            Document = document;
            return;
        }

        var normalized = source.Replace("\r\n", "\n");
        var parts = normalized.Split(["\n\n"], StringSplitOptions.None);
        foreach (var part in parts)
        {
            AddParagraph(document, part.Trim('\n'));
        }

        Document = document;
    }

    private void AddParagraph(FlowDocument document, string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            document.Blocks.Add(new Paragraph { Margin = new Thickness(0, 0, 0, 10) });
            return;
        }

        var position = 0;
        var paragraph = CreateParagraph();
        foreach (Match match in LatexTokenRegex.Matches(source))
        {
            if (match.Index > position)
            {
                paragraph.Inlines.Add(CreateRun(source[position..match.Index]));
            }

            var displayFormula = match.Groups["display"].Success;
            var formula = displayFormula ? match.Groups["display"].Value : match.Groups["inline"].Value;
            if (displayFormula)
            {
                if (paragraph.Inlines.Count > 0)
                {
                    document.Blocks.Add(paragraph);
                    paragraph = CreateParagraph();
                }

                document.Blocks.Add(CreateFormulaParagraph(formula));
            }
            else
            {
                paragraph.Inlines.Add(CreateInlineFormula(formula));
            }

            position = match.Index + match.Length;
        }

        if (position < source.Length)
        {
            paragraph.Inlines.Add(CreateRun(source[position..]));
        }

        if (paragraph.Inlines.Count > 0)
        {
            document.Blocks.Add(paragraph);
        }
    }

    private Paragraph CreateParagraph()
        => new()
        {
            Margin = new Thickness(0, 0, 0, 10),
            LineHeight = TextLineHeight,
            FontFamily = FontFamily,
            FontSize = FontSize,
            Foreground = Foreground
        };

    private Run CreateRun(string text)
        => new(text.Replace("\n", Environment.NewLine))
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            Foreground = Foreground
        };

    private InlineUIContainer CreateInlineFormula(string formula)
        => new(CreateFormulaControl(formula, FormulaFontSize))
        {
            BaselineAlignment = BaselineAlignment.Center
        };

    private Paragraph CreateFormulaParagraph(string formula)
    {
        var paragraph = CreateParagraph();
        paragraph.TextAlignment = TextAlignment.Center;
        paragraph.Margin = new Thickness(0, 8, 0, 12);
        paragraph.Inlines.Add(new InlineUIContainer(CreateFormulaControl(formula, FormulaFontSize + 4))
        {
            BaselineAlignment = BaselineAlignment.Center
        });
        return paragraph;
    }

    private UIElement CreateFormulaControl(string formula, double fontSize)
    {
        var normalized = NormalizeFormula(formula);
        try
        {
            return new FormulaControl
            {
                Formula = normalized,
                FontSize = fontSize,
                Foreground = Foreground,
                Margin = new Thickness(3, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        catch
        {
            return new TextBlock
            {
                Text = $"\\({normalized}\\)",
                FontFamily = new FontFamily("Consolas"),
                FontSize = FontSize,
                Foreground = Foreground,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private static string NormalizeFormula(string formula)
        => formula.Trim();

    private static string AutoFormatPlainMath(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return source;

        var builder = new StringBuilder();
        var position = 0;
        foreach (Match match in LatexTokenRegex.Matches(source))
        {
            if (match.Index > position)
            {
                builder.Append(AutoFormatPlainSegment(source[position..match.Index]));
            }

            builder.Append(match.Value);
            position = match.Index + match.Length;
        }

        if (position < source.Length)
        {
            builder.Append(AutoFormatPlainSegment(source[position..]));
        }

        return builder.ToString();
    }

    private static string AutoFormatPlainSegment(string text)
    {
        var formatted = SqrtCallRegex.Replace(text, @"\sqrt{$1}");
        formatted = PowerRegex.Replace(formatted, "${base}^{${power}}");
        formatted = EquationRegex.Replace(formatted, match => WrapFormula(match.Value));
        formatted = FormatOutsideLatexTokens(formatted, segment => StandaloneMathRegex.Replace(segment, match => WrapFormula(match.Value)));
        return formatted;
    }

    private static string FormatOutsideLatexTokens(string source, Func<string, string> formatter)
    {
        var builder = new StringBuilder();
        var position = 0;
        foreach (Match match in LatexTokenRegex.Matches(source))
        {
            if (match.Index > position)
            {
                builder.Append(formatter(source[position..match.Index]));
            }

            builder.Append(match.Value);
            position = match.Index + match.Length;
        }

        if (position < source.Length)
        {
            builder.Append(formatter(source[position..]));
        }

        return builder.ToString();
    }

    private static string WrapFormula(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains(@"\(") || value.Contains(@"\[")) return value;

        var leading = value.Length - value.TrimStart().Length;
        var trailing = value.Length - value.TrimEnd().Length;
        var prefix = value[..leading];
        var suffix = trailing == 0 ? string.Empty : value[^trailing..];
        var expression = value.Trim();
        if (expression.Length == 0) return value;

        while (expression.Length > 0 && expression[^1] is '.' or ',' or ';' or ':')
        {
            suffix = expression[^1] + suffix;
            expression = expression[..^1].TrimEnd();
        }

        expression = expression
            .Replace(">=", @"\ge ")
            .Replace("<=", @"\le ")
            .Replace("≥", @"\ge ")
            .Replace("≤", @"\le ")
            .Replace("->", @"\to ")
            .Replace("+-", @"\pm ")
            .Replace("*", @"\cdot ");

        return $@"{prefix}\({expression}\){suffix}";
    }

    [GeneratedRegex(@"\\\[(?<display>.*?)\\\]|\\\((?<inline>.*?)\\\)", RegexOptions.Singleline)]
    private static partial Regex LatexRegex();

    [GeneratedRegex(@"sqrt\(([^)]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex SqrtRegex();

    [GeneratedRegex(@"(?<base>[A-Za-z]\w*|\d+|\))\^(?<power>-?\d+)")]
    private static partial Regex PowerExpressionRegex();

    [GeneratedRegex(@"(?<![\\\w])(?<expr>(?:\\sqrt\{[^}]+\}|[A-Za-z]\w*|\d+(?:[.,]\d+)?|\([^)]+\)|[+\-*/^{}.,;%\s]){1,90}(?:=|<=|>=|<|>|\\leq?|\\geq?)(?:\\sqrt\{[^}]+\}|[A-Za-z]\w*|\d+(?:[.,]\d+)?|\([^)]+\)|[+\-*/^{}.,;%\s]){1,90})(?![\w])")]
    private static partial Regex EquationExpressionRegex();

    [GeneratedRegex(@"(?<![\\\w])(?:\\sqrt\{[^}]+\}|(?:[A-Za-z]\w*|\d+|\))\^\{[^}]+\}|\d+\s*/\s*\d+)(?![\w])")]
    private static partial Regex StandaloneExpressionRegex();
}
