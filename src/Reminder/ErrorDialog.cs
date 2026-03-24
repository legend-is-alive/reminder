using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Reminder;

internal static class ErrorDialog
{
    internal static void Show(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ShowCore(message);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ShowCore(message));
        }
    }

    private static void ShowCore(string message)
    {
        var window = new Window
        {
            Title = "Error",
            Width = 420,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = false
        };
        UIHelper.SetIcon(window);

        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 16 };

        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 4)
        };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "❌",
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Something went wrong",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(headerPanel);

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#D32F2F"))
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(20, 8),
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(4)
        };
        okButton.Click += (_, _) => window.Close();
        panel.Children.Add(okButton);

        window.Content = panel;
        window.Show();
    }
}
