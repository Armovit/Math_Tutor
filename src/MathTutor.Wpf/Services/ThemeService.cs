using System.Windows;

namespace MathTutor.Wpf.Services;

public sealed class ThemeService : IThemeService
{
    private const string LightTheme = "Resources/DesignSystem/Themes/LightTheme.xaml";
    private const string DarkTheme = "Resources/DesignSystem/Themes/DarkTheme.xaml";

    public bool IsDarkTheme { get; private set; }
    public string CurrentThemeName => IsDarkTheme ? "Тёмная" : "Светлая";

    public void ApplyLightTheme() => ApplyTheme(LightTheme, false);

    public void ApplyDarkTheme() => ApplyTheme(DarkTheme, true);

    private void ApplyTheme(string source, bool isDark)
    {
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var currentTheme = dictionaries.FirstOrDefault(x => x.Source?.OriginalString.Contains("Resources/DesignSystem/Themes/", StringComparison.OrdinalIgnoreCase) == true);
        if (currentTheme is not null)
        {
            dictionaries.Remove(currentTheme);
        }

        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
        IsDarkTheme = isDark;
    }
}
