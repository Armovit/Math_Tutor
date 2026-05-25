using System.Windows.Input;

namespace MathTutor.Wpf.ViewModels;

public sealed record NavigationItemViewModel(string Title, string Icon, ICommand Command, bool IsActive = false);
