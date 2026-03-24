using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Reminder;

internal static class UIHelper
{
    internal static void SetIcon(Window window)
    {
        try
        {
            window.Icon = new WindowIcon(AppConstants.IconPath);
        }
        catch
        {
            // Icon is non-critical; ignore failures silently.
        }
    }

    internal static void PostOnUI(Action action, string? errorContext = null)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var prefix = errorContext is not null ? $"{errorContext}: " : "";
                ErrorDialog.Show($"{prefix}{ex.Message}");
            }
        });
    }

    internal static async Task ShowMessageAsync(Window owner, string message, string title = "Info", string icon = "ℹ️")
    {
        var dialog = CreateDialogWindow(title, 380, 200);

        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
        panel.Children.Add(CreateDialogHeader(icon, title));
        panel.Children.Add(CreateDialogBody(message));

        var okButton = new Button
        {
            Content = "OK",
            Padding = new Thickness(20, 10),
            HorizontalAlignment = HorizontalAlignment.Right,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(4)
        };
        okButton.Click += (_, _) => dialog.Close();
        panel.Children.Add(okButton);

        dialog.Content = panel;
        await dialog.ShowDialog(owner);
    }

    internal static async Task<bool> ShowConfirmAsync(Window owner, string message, string title = "Confirm")
    {
        var dialog = CreateDialogWindow(title, 420, 220);

        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
        panel.Children.Add(CreateDialogHeader("⚠️", title));
        panel.Children.Add(CreateDialogBody(message));

        var result = false;

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 10),
            CornerRadius = new CornerRadius(4)
        };
        cancelButton.Click += (_, _) => dialog.Close();

        var yesButton = new Button
        {
            Content = "Yes",
            Padding = new Thickness(20, 10),
            Background = new SolidColorBrush(Color.Parse("#F44336")),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(4)
        };
        yesButton.Click += (_, _) => { result = true; dialog.Close(); };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(yesButton);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        await dialog.ShowDialog(owner);
        return result;
    }

    private static Window CreateDialogWindow(string title, int width, int height)
    {
        var window = new Window
        {
            Title = title,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        SetIcon(window);
        return window;
    }

    private static Panel CreateDialogHeader(string icon, string title)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 4)
        };
        panel.Children.Add(new TextBlock
        {
            Text = icon,
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        return panel;
    }

    private static TextBlock CreateDialogBody(string message) => new()
    {
        Text = message,
        TextWrapping = TextWrapping.Wrap,
        FontSize = 13,
        Foreground = new SolidColorBrush(Color.Parse("#444444"))
    };
}
