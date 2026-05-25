using System.Windows;
using MathTutor.Application.Abstractions;
using MathTutor.Infrastructure;
using MathTutor.Wpf.Navigation;
using MathTutor.Wpf.Services;
using MathTutor.Wpf.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MathTutor.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(-1);
        };

        host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                var profile = Environment.GetEnvironmentVariable("MATH_TUTOR_DB_PROFILE");
                if (!string.IsNullOrWhiteSpace(profile))
                {
                    config.AddJsonFile($"appsettings.{profile}.json", optional: true, reloadOnChange: true);
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure(context.Configuration);
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ShellViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<AdminDashboardViewModel>();
                services.AddTransient<StudentDashboardViewModel>();
                services.AddTransient<MaterialsManagementViewModel>();
                services.AddTransient<TaskManagementViewModel>();
                services.AddTransient<StudentMaterialsViewModel>();
                services.AddTransient<TheoryViewModel>();
                services.AddTransient<ProgressViewModel>();
                services.AddTransient<ReviewsViewModel>();
                services.AddTransient<NotificationsViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<StatisticsViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        try
        {
            await host.StartAsync();
            using (var scope = host.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeAsync();
            }

            var window = host.Services.GetRequiredService<MainWindow>();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Не удалось запустить приложение", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (host is not null)
        {
            var navigation = host.Services.GetService<INavigationService>();
            if (navigation is IDisposable disposableNavigation)
            {
                disposableNavigation.Dispose();
            }

            await host.StopAsync(TimeSpan.FromSeconds(3));
            host.Dispose();
        }
        base.OnExit(e);
    }
}
