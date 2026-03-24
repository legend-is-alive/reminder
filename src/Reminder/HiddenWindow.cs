using Avalonia.Controls;
using H.NotifyIcon;

namespace Reminder;

public sealed class HiddenWindow : Window
{
    private readonly TrayIcon _tray;
    private bool _disposed;

    public HiddenWindow()
    {
        ShowInTaskbar = false;
        WindowState = WindowState.Minimized;
        UIHelper.SetIcon(this);

        _tray = new TrayIcon
        {
            ToolTipText = GetTooltipText(),
            Icon = new WindowIcon(AppConstants.IconPath),
            IsVisible = true,
            Menu = new NativeMenu
            {
                Items =
                {
                    new NativeMenuItem("📋 View Reminders") { Command = new RelayCommand(OpenReminderList) },
                    new NativeMenuItem("➕ Add Reminder") { Command = new RelayCommand(OpenAddReminderWindow) },
                    new NativeMenuItemSeparator(),
                    new NativeMenuItem("🚪 Exit") { Command = new RelayCommand(ExitApplication) }
                }
            }
        };

        App.Service.OnReminderTriggered += ShowReminderPopup;
        App.Service.OnRemindersChanged += UpdateTooltip;
    }

    private string GetTooltipText()
    {
        var count = App.Service.GetPendingCount();
        return count switch
        {
            0 => "Reminder — No pending reminders",
            1 => "Reminder — 1 pending reminder",
            _ => $"Reminder — {count} pending reminders"
        };
    }

    private void UpdateTooltip()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _tray.ToolTipText = GetTooltipText();
            }
            catch
            {
                // Tooltip update is non-critical
            }
        });
    }

    private void OpenReminderList() =>
        UIHelper.PostOnUI(ReminderListWindow.ShowSingleInstance, "Error opening reminder list");

    private void OpenAddReminderWindow() =>
        UIHelper.PostOnUI(() => new ReminderFormWindow().Show(), "Error opening add reminder window");

    private async void ExitApplication()
    {
        try
        {
            await App.Service.DisposeAsync();
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error during shutdown: {ex.Message}");
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    private void ShowReminderPopup(ReminderModel reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        UIHelper.PostOnUI(() => new ReminderPopup(reminder).Show(), "Error showing reminder popup");
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_disposed)
        {
            App.Service.OnReminderTriggered -= ShowReminderPopup;
            App.Service.OnRemindersChanged -= UpdateTooltip;
            _tray?.Dispose();
            _disposed = true;
        }

        base.OnClosed(e);
    }
}
