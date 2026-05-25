using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathTutor.Application.Abstractions;
using MathTutor.Application.DTOs;

namespace MathTutor.Wpf.ViewModels;

public sealed partial class NotificationsViewModel(INotificationService notificationService, ISessionService sessionService) : ViewModelBase, ILoadableViewModel
{
    [ObservableProperty]
    private NotificationDto? selectedNotification;

    public ObservableCollection<NotificationDto> Notifications { get; } = new();
    public override string Title => "Уведомления";

    [RelayCommand]
    public Task LoadAsync() => RunAsync(async () =>
    {
        Notifications.Clear();
        if (sessionService.CurrentUser is null) return;
        foreach (var notification in await notificationService.GetForUserAsync(sessionService.CurrentUser.Id)) Notifications.Add(notification);
    });

    [RelayCommand]
    private Task MarkAsReadAsync() => RunAsync(async () =>
    {
        if (SelectedNotification is null) return;
        var result = await notificationService.MarkAsReadAsync(SelectedNotification.Id);
        StatusMessage = result.Message;
        await LoadAsync();
    });
}
