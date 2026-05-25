namespace MathTutor.Wpf.Services;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    string CurrentThemeName { get; }
    void ApplyLightTheme();
    void ApplyDarkTheme();
}
