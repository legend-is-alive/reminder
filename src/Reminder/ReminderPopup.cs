using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Reminder;

public sealed class ReminderPopup : Window
{
    private const int PopupWidth = 400;
    private const int PopupHeight = 280;
    private const int ScreenMargin = 20;

    private static readonly int[] SnoozeOptions = [5, 15, 30, 60];

    private readonly ReminderModel _reminder;

    public ReminderPopup(ReminderModel reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        _reminder = reminder;
        ReminderAudio.PlayNotificationSound();

        Width = PopupWidth;
        Height = PopupHeight;
        Topmost = true;
        CanResize = false;
        Title = "Reminder";
        SystemDecorations = SystemDecorations.BorderOnly;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
        UIHelper.SetIcon(this);

        PositionWindowOnScreen();
        BuildContent();
    }

    private void PositionWindowOnScreen()
    {
        try
        {
            var screen = Screens.Primary?.WorkingArea;
            if (screen.HasValue)
            {
                Position = new PixelPoint(
                    screen.Value.Width - (int)Width - ScreenMargin,
                    screen.Value.Height - (int)Height - ScreenMargin);
            }
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error positioning popup: {ex.Message}");
        }
    }

    private void BuildContent()
    {
        var priorityColor = ReminderModel.GetPriorityColor(_reminder.Priority);

        var mainBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FAFAFA")),
            BorderBrush = new SolidColorBrush(Color.Parse(priorityColor)),
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(6)
        };

        var panel = new StackPanel { Margin = new Thickness(18), Spacing = 10 };

        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerPanel.Children.Add(new TextBlock
        {
            Text = "🔔",
            FontSize = 24,
            VerticalAlignment = VerticalAlignment.Center
        });

        var titlePanel = new StackPanel { Spacing = 2 };
        titlePanel.Children.Add(new TextBlock
        {
            Text = _reminder.Subject,
            FontSize = 17,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#212121"))
        });

        var metaPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        metaPanel.Children.Add(new TextBlock
        {
            Text = ReminderModel.FormatTimeRelative(_reminder.Time),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#757575"))
        });

        if (_reminder.Priority != ReminderPriority.Normal)
        {
            metaPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse(priorityColor)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                Child = new TextBlock
                {
                    Text = _reminder.Priority.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold
                }
            });
        }

        if (_reminder.IsRecurring)
        {
            metaPanel.Children.Add(new TextBlock
            {
                Text = $"🔁 {_reminder.Recurrence}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#7B1FA2"))
            });
        }

        titlePanel.Children.Add(metaPanel);
        headerPanel.Children.Add(titlePanel);
        panel.Children.Add(headerPanel);

        panel.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#E0E0E0")),
            Margin = new Thickness(0, 2)
        });

        if (!string.IsNullOrWhiteSpace(_reminder.Description))
        {
            var descriptionScroll = new ScrollViewer
            {
                MaxHeight = 50,
                Content = new TextBlock
                {
                    Text = _reminder.Description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#555555"))
                }
            };
            panel.Children.Add(descriptionScroll);
        }

        var snoozeLabel = new TextBlock
        {
            Text = "Snooze for:",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            Margin = new Thickness(0, 4, 0, 0)
        };
        panel.Children.Add(snoozeLabel);

        var snoozePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        foreach (var minutes in SnoozeOptions)
        {
            var label = minutes >= 60 ? $"{minutes / 60}h" : $"{minutes}m";
            var snoozeBtn = new Button
            {
                Content = $"⏰ {label}",
                Padding = new Thickness(10, 6),
                Background = new SolidColorBrush(Color.Parse("#FF9800")),
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                CornerRadius = new CornerRadius(4)
            };
            var capturedMinutes = minutes;
            snoozeBtn.Click += async (_, _) => await OnSnoozeAsync(capturedMinutes);
            snoozePanel.Children.Add(snoozeBtn);
        }

        panel.Children.Add(snoozePanel);

        var actionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 6, 0, 0)
        };

        var dismissButton = new Button
        {
            Content = "Dismiss",
            Padding = new Thickness(12, 7),
            CornerRadius = new CornerRadius(4),
            FontSize = 12
        };
        dismissButton.Click += (_, _) => Close();

        var doneButton = new Button
        {
            Content = "✓ Mark Done",
            Padding = new Thickness(14, 7),
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            CornerRadius = new CornerRadius(4)
        };
        doneButton.Click += async (_, _) => await OnDoneAsync();

        actionPanel.Children.Add(dismissButton);
        actionPanel.Children.Add(doneButton);
        panel.Children.Add(actionPanel);

        mainBorder.Child = panel;
        Content = mainBorder;
    }

    private async Task OnSnoozeAsync(int minutes)
    {
        try
        {
            await App.Service.SnoozeReminderAsync(_reminder, minutes);
            Close();
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error snoozing reminder: {ex.Message}");
        }
    }

    private async Task OnDoneAsync()
    {
        try
        {
            await App.Service.MarkReminderAsCompletedAsync(_reminder);
            Close();
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error completing reminder: {ex.Message}");
        }
    }
}
