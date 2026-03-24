using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Reminder;

public sealed class ReminderFormWindow : Window
{
    private const int WindowWidth = 440;
    private const int WindowHeight = 780;

    private readonly ReminderModel? _originalReminder;
    private readonly TextBox _subject;
    private readonly TextBox _description;
    private readonly DatePicker _datePicker;
    private readonly TimePicker _timePicker;
    private readonly ComboBox _priorityPicker;
    private readonly AutoCompleteBox _categoryBox;
    private readonly ComboBox _recurrencePicker;
    private readonly Button _saveButton;

    private bool IsEditMode => _originalReminder is not null;

    public ReminderFormWindow(ReminderModel? existingReminder = null)
    {
        _originalReminder = existingReminder;

        Width = WindowWidth;
        Height = WindowHeight;
        Title = IsEditMode ? "Edit Reminder" : "New Reminder";
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UIHelper.SetIcon(this);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 10 };

        panel.Children.Add(new TextBlock
        {
            Text = IsEditMode ? "✏️ Edit Reminder" : "➕ Create New Reminder",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(CreateSeparator());

        panel.Children.Add(CreateFieldLabel("📝 Subject"));
        _subject = new TextBox
        {
            Text = existingReminder?.Subject ?? string.Empty,
            Watermark = "What do you need to remember?",
            MaxLength = 200
        };
        panel.Children.Add(_subject);

        panel.Children.Add(CreateFieldLabel("📄 Description"));
        _description = new TextBox
        {
            Text = existingReminder?.Description ?? string.Empty,
            Watermark = "Add details (optional)",
            AcceptsReturn = true,
            Height = 80,
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(_description);

        panel.Children.Add(CreateSeparator());

        panel.Children.Add(CreateFieldLabel("📅 Date"));
        _datePicker = new DatePicker
        {
            SelectedDate = existingReminder?.Time.Date ?? DateTime.Today,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Children.Add(_datePicker);

        panel.Children.Add(CreateFieldLabel("🕐 Time"));
        _timePicker = new TimePicker
        {
            SelectedTime = existingReminder?.Time.TimeOfDay
                ?? TimeSpan.FromMinutes(Math.Ceiling(DateTime.Now.TimeOfDay.TotalMinutes / 5) * 5 + 5),
            ClockIdentifier = "24HourClock",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Children.Add(_timePicker);

        panel.Children.Add(CreateSeparator());

        panel.Children.Add(CreateFieldLabel("⚡ Priority"));
        _priorityPicker = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[]
            {
                ReminderModel.GetPriorityLabel(ReminderPriority.Low),
                ReminderModel.GetPriorityLabel(ReminderPriority.Normal),
                ReminderModel.GetPriorityLabel(ReminderPriority.High),
                ReminderModel.GetPriorityLabel(ReminderPriority.Urgent)
            },
            SelectedIndex = existingReminder is not null ? (int)existingReminder.Priority : 1
        };
        panel.Children.Add(_priorityPicker);

        panel.Children.Add(CreateFieldLabel("🏷️ Category"));
        _categoryBox = new AutoCompleteBox
        {
            Watermark = "e.g. Work, Personal, Health...",
            Text = existingReminder?.Category ?? string.Empty,
            ItemsSource = App.Service.GetCategories(),
            FilterMode = AutoCompleteFilterMode.Contains
        };
        panel.Children.Add(_categoryBox);

        panel.Children.Add(CreateFieldLabel("🔁 Repeat"));
        _recurrencePicker = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "None", ReminderModel.GetRecurrenceLabel(RecurrenceType.Daily), ReminderModel.GetRecurrenceLabel(RecurrenceType.Weekly), ReminderModel.GetRecurrenceLabel(RecurrenceType.Monthly) },
            SelectedIndex = existingReminder is not null ? (int)existingReminder.Recurrence : 0
        };
        panel.Children.Add(_recurrencePicker);

        panel.Children.Add(CreateSeparator());

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(20, 10),
            CornerRadius = new CornerRadius(4)
        };
        cancelButton.Click += (_, _) => Close();

        _saveButton = new Button
        {
            Content = IsEditMode ? "💾 Save Changes" : "💾 Create Reminder",
            Padding = new Thickness(20, 10),
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(4)
        };
        _saveButton.Click += SaveReminderAsync;

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(_saveButton);
        panel.Children.Add(buttonPanel);

        scrollViewer.Content = panel;
        Content = scrollViewer;

        _subject.AttachedToVisualTree += (_, _) => _subject.Focus();
    }

    private static TextBlock CreateFieldLabel(string text) => new()
    {
        Text = text,
        FontWeight = FontWeight.SemiBold,
        FontSize = 13,
        Margin = new Thickness(0, 4, 0, 0)
    };

    private static Border CreateSeparator() => new()
    {
        Height = 1,
        Background = new SolidColorBrush(Color.Parse("#E0E0E0")),
        Margin = new Thickness(0, 4)
    };

    private async void SaveReminderAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            _saveButton.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(_subject.Text))
            {
                ErrorDialog.Show("Subject is required");
                return;
            }

            var selectedDate = _datePicker.SelectedDate?.Date;
            var selectedTime = _timePicker.SelectedTime;

            if (selectedDate is null || selectedTime is null)
            {
                ErrorDialog.Show("Please select both date and time");
                return;
            }

            var reminderTime = selectedDate.Value + selectedTime.Value;

            if (reminderTime <= DateTime.Now)
            {
                ErrorDialog.Show("Reminder time must be in the future");
                return;
            }

            var priority = (ReminderPriority)(_priorityPicker.SelectedIndex >= 0 ? _priorityPicker.SelectedIndex : 1);
            var recurrence = (RecurrenceType)(_recurrencePicker.SelectedIndex >= 0 ? _recurrencePicker.SelectedIndex : 0);
            var category = string.IsNullOrWhiteSpace(_categoryBox.Text) ? null : _categoryBox.Text.Trim();

            var reminder = new ReminderModel
            {
                Id = _originalReminder?.Id ?? Guid.NewGuid(),
                Time = reminderTime,
                Subject = _subject.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(_description.Text) ? null : _description.Text.Trim(),
                Priority = priority,
                Category = category,
                Recurrence = recurrence,
                CreatedAt = _originalReminder?.CreatedAt ?? DateTime.Now
            };

            if (_originalReminder is not null)
            {
                await App.Service.UpdateReminderAsync(_originalReminder, reminder);
            }
            else
            {
                await App.Service.AddReminderAsync(reminder);
            }

            Close();
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error saving reminder: {ex.Message}");
        }
        finally
        {
            _saveButton.IsEnabled = true;
        }
    }
}
