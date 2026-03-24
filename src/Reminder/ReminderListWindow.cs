using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Reminder;

public sealed class ReminderListWindow : Window
{
    private const int WindowWidth = 780;
    private const int WindowHeight = 600;

    private static ReminderListWindow? _instance;

    private readonly ListBox _reminderListBox;
    private readonly TextBlock _emptyMessage;
    private readonly TextBlock _statsText;
    private readonly CheckBox _showDoneCheckBox;
    private readonly TextBox _searchBox;
    private readonly ComboBox _priorityFilter;
    private readonly ComboBox _categoryFilter;
    private readonly ComboBox _sortPicker;

    public static void ShowSingleInstance()
    {
        if (_instance is { IsVisible: true })
        {
            _instance.Activate();
            return;
        }

        _instance = new ReminderListWindow();
        _instance.Closed += (_, _) => _instance = null;
        _instance.Show();
        _instance.Activate();
    }

    private ReminderListWindow()
    {
        Width = WindowWidth;
        Height = WindowHeight;
        Title = "Reminder — Manage Your Reminders";
        CanResize = true;
        MinWidth = 600;
        MinHeight = 450;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UIHelper.SetIcon(this);

        var mainPanel = new DockPanel { Margin = new Thickness(12) };

        var headerPanel = CreateHeader();
        DockPanel.SetDock(headerPanel, Dock.Top);
        mainPanel.Children.Add(headerPanel);

        _searchBox = new TextBox
        {
            Watermark = "🔍 Search reminders...",
            Margin = new Thickness(0, 8, 0, 0)
        };
        DockPanel.SetDock(_searchBox, Dock.Top);
        mainPanel.Children.Add(_searchBox);

        _showDoneCheckBox = new CheckBox
        {
            Content = "Show completed",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };

        _priorityFilter = new ComboBox
        {
            ItemsSource = new[] { "All Priorities", "🔵 Low", "🟢 Normal", "🟠 High", "🔴 Urgent" },
            SelectedIndex = 0,
            MinWidth = 130,
            VerticalAlignment = VerticalAlignment.Center
        };

        _categoryFilter = new ComboBox
        {
            MinWidth = 130,
            VerticalAlignment = VerticalAlignment.Center
        };

        _sortPicker = new ComboBox
        {
            ItemsSource = new[] { "Sort: Time ↑", "Sort: Time ↓", "Sort: Priority ↓", "Sort: Name A-Z" },
            SelectedIndex = 0,
            MinWidth = 130,
            VerticalAlignment = VerticalAlignment.Center
        };

        var buttonBar = CreateButtonBar();
        DockPanel.SetDock(buttonBar, Dock.Top);
        mainPanel.Children.Add(buttonBar);

        _statsText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#666666")),
            Margin = new Thickness(2, 8, 0, 0)
        };
        DockPanel.SetDock(_statsText, Dock.Top);
        mainPanel.Children.Add(_statsText);

        var listContainer = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(4),
            Margin = new Thickness(0, 8, 0, 0)
        };

        _emptyMessage = new TextBlock
        {
            Text = "No reminders yet. Press Ctrl+N or click 'Add New' to create one.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Foreground = Brushes.Gray,
            IsVisible = false
        };

        _reminderListBox = new ListBox
        {
            SelectionMode = SelectionMode.Single
        };

        _reminderListBox.DoubleTapped += OnListDoubleTapped;

        var scrollViewer = new ScrollViewer
        {
            Content = _reminderListBox
        };

        var contentGrid = new Grid();
        contentGrid.Children.Add(scrollViewer);
        contentGrid.Children.Add(_emptyMessage);

        listContainer.Child = contentGrid;
        mainPanel.Children.Add(listContainer);

        Content = mainPanel;

        KeyDown += OnWindowKeyDown;

        _searchBox.TextChanged += (_, _) => LoadReminders();
        _showDoneCheckBox.IsCheckedChanged += (_, _) => LoadReminders();
        _priorityFilter.SelectionChanged += (_, _) => LoadReminders();
        _categoryFilter.SelectionChanged += (_, _) => LoadReminders();
        _sortPicker.SelectionChanged += (_, _) => LoadReminders();
        RefreshCategoryFilter();

        App.Service.OnRemindersChanged += () =>
            Avalonia.Threading.Dispatcher.UIThread.Post(LoadReminders);

        LoadReminders();
    }

    private Panel CreateHeader()
    {
        var panel = new DockPanel { Margin = new Thickness(0, 0, 0, 2) };

        var titlePanel = new StackPanel();
        titlePanel.Children.Add(new TextBlock
        {
            Text = "📋 Your Reminders",
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 2)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Manage, organize, and track all your reminders — Ctrl+N to add, Del to remove",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#888888"))
        });

        panel.Children.Add(titlePanel);
        return panel;
    }

    private Panel CreateButtonBar()
    {
        var outerPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var row1 = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        row1.Children.Add(CreateToolButton("➕ Add New", "#4CAF50", OpenAddWindow, "Ctrl+N"));
        row1.Children.Add(CreateToolButton("✏️ Edit", "#2196F3", EditSelectedReminder, "Double-click"));
        row1.Children.Add(CreateToolButton("✓ Done", "#009688", MarkSelectedDone));
        row1.Children.Add(CreateToolButton("↩️ Reopen", "#7B1FA2", ReopenSelectedReminder));
        row1.Children.Add(CreateToolButton("🗑️ Delete", "#F44336", DeleteSelectedReminder, "Del"));
        row1.Children.Add(CreateToolButton("🔄 Refresh", null, () => LoadReminders(), "F5"));
        row1.Children.Add(CreateToolButton("🧹 Clear Done", "#795548", ClearCompleted));

        outerPanel.Children.Add(row1);

        var row2 = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        row2.Children.Add(_priorityFilter);
        row2.Children.Add(_categoryFilter);
        row2.Children.Add(_sortPicker);
        row2.Children.Add(_showDoneCheckBox);

        outerPanel.Children.Add(row2);

        return outerPanel;
    }

    private static Button CreateToolButton(string content, string? bgColor, Action onClick, string? shortcutHint = null)
    {
        var button = new Button
        {
            Content = content,
            Padding = new Thickness(12, 7),
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            CornerRadius = new CornerRadius(4)
        };

        if (shortcutHint is not null)
        {
            ToolTip.SetTip(button, shortcutHint);
        }

        if (bgColor is not null)
        {
            button.Background = new SolidColorBrush(Color.Parse(bgColor));
            button.Foreground = Brushes.White;
        }

        button.Click += (_, _) => onClick();
        return button;
    }

    private void LoadReminders()
    {
        var reminders = App.Service.GetAllReminders();
        if (_showDoneCheckBox.IsChecked == true)
        {
            reminders.AddRange(App.Service.GetCompletedReminders());
        }

        var searchText = _searchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            reminders = reminders.Where(r =>
                r.Subject.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Category?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        if (_priorityFilter.SelectedIndex > 0)
        {
            var filterPriority = (ReminderPriority)(_priorityFilter.SelectedIndex - 1);
            reminders = reminders.Where(r => r.Priority == filterPriority).ToList();
        }

        if (_categoryFilter.SelectedIndex > 0 && _categoryFilter.SelectedItem is string selectedCategory)
        {
            reminders = reminders.Where(r =>
                string.Equals(r.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        reminders = (_sortPicker.SelectedIndex switch
        {
            1 => reminders.OrderByDescending(r => r.Time),
            2 => reminders.OrderByDescending(r => r.Priority).ThenBy(r => r.Time),
            3 => reminders.OrderBy(r => r.Subject, StringComparer.OrdinalIgnoreCase),
            _ => reminders.OrderBy(r => r.Time)
        }).ToList();

        _reminderListBox.Items.Clear();

        if (reminders.Count == 0)
        {
            _emptyMessage.Text = !string.IsNullOrEmpty(searchText) || _priorityFilter.SelectedIndex > 0
                ? "No reminders match your search or filters."
                : _showDoneCheckBox.IsChecked == true
                    ? "No reminders yet."
                    : "No reminders yet. Press Ctrl+N or click 'Add New' to create one.";
            _emptyMessage.IsVisible = true;
        }
        else
        {
            _emptyMessage.IsVisible = false;
        }

        foreach (var reminder in reminders)
        {
            _reminderListBox.Items.Add(CreateReminderListItem(reminder));
        }

        UpdateStats();
    }

    private void UpdateStats()
    {
        var stats = App.Service.GetStatistics();
        var parts = new List<string>
        {
            $"📌 {stats.Pending} pending"
        };

        if (stats.Overdue > 0)
            parts.Add($"⚠️ {stats.Overdue} overdue");

        if (stats.DueToday > 0)
            parts.Add($"📅 {stats.DueToday} due today");

        parts.Add($"✅ {stats.Completed} completed");
        parts.Add($"📊 {stats.Total} total");

        _statsText.Text = string.Join("   ·   ", parts);
    }

    private Border CreateReminderListItem(ReminderModel reminder)
    {
        var priorityColor = ReminderModel.GetPriorityColor(reminder.Priority);

        var backgroundColor = reminder.IsCompleted
            ? "#E8F5E9"
            : reminder.IsTriggered
                ? "#FFF3E0"
                : reminder.IsOverdue
                    ? "#FFEBEE"
                    : "#F5F5F5";

        var hoverColor = reminder.IsCompleted
            ? "#DDEFE0"
            : reminder.IsTriggered
                ? "#FFE8CC"
                : reminder.IsOverdue
                    ? "#FFCDD2"
                    : "#E3F2FD";

        var border = new Border
        {
            Margin = new Thickness(0, 3, 0, 3),
            Padding = new Thickness(14, 12),
            Background = new SolidColorBrush(Color.Parse(backgroundColor)),
            CornerRadius = new CornerRadius(6),
            BorderBrush = new SolidColorBrush(Color.Parse("#E0E0E0")),
            BorderThickness = new Thickness(1),
            Tag = reminder
        };

        var mainPanel = new DockPanel();

        var priorityBar = new Border
        {
            Width = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.Parse(priorityColor)),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        DockPanel.SetDock(priorityBar, Dock.Left);
        mainPanel.Children.Add(priorityBar);

        var contentPanel = new StackPanel { Spacing = 5 };

        var topRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        topRow.Children.Add(new TextBlock
        {
            Text = reminder.Subject,
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 400
        });

        if (reminder.Priority != ReminderPriority.Normal)
        {
            topRow.Children.Add(CreateBadge(
                reminder.Priority.ToString(),
                priorityColor));
        }

        if (reminder.IsRecurring)
        {
            topRow.Children.Add(CreateBadge("🔁 " + reminder.Recurrence, "#7B1FA2"));
        }

        if (reminder.IsCompleted)
        {
            topRow.Children.Add(CreateBadge("Done", "#4CAF50"));
        }
        else if (reminder.IsTriggered)
        {
            topRow.Children.Add(CreateBadge("Due", "#FF9800"));
        }
        else if (reminder.IsOverdue)
        {
            topRow.Children.Add(CreateBadge("Overdue", "#F44336"));
        }

        contentPanel.Children.Add(topRow);

        if (!string.IsNullOrWhiteSpace(reminder.Description))
        {
            contentPanel.Children.Add(new TextBlock
            {
                Text = reminder.Description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#666666")),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 600
            });
        }

        var bottomRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Margin = new Thickness(0, 2, 0, 0) };

        bottomRow.Children.Add(new TextBlock
        {
            Text = $"⏰ {ReminderModel.FormatTimeRelative(reminder.Time)}",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#888888"))
        });

        var timeRemaining = reminder.Time - DateTime.Now;
        if (!reminder.IsCompleted && timeRemaining.TotalSeconds > 0)
        {
            bottomRow.Children.Add(new TextBlock
            {
                Text = $"({ReminderModel.GetTimeRemainingText(timeRemaining)})",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#4CAF50")),
                FontWeight = FontWeight.SemiBold
            });
        }
        else if (reminder.IsCompleted && reminder.CompletedAt is { } completedAt)
        {
            bottomRow.Children.Add(new TextBlock
            {
                Text = $"✅ Completed {ReminderModel.FormatTimeRelative(completedAt)}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#2E7D32")),
                FontWeight = FontWeight.SemiBold
            });
        }

        if (!string.IsNullOrWhiteSpace(reminder.Category))
        {
            bottomRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#E8EAF6")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                Child = new TextBlock
                {
                    Text = $"🏷️ {reminder.Category}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#3F51B5"))
                }
            });
        }

        contentPanel.Children.Add(bottomRow);

        mainPanel.Children.Add(contentPanel);
        border.Child = mainPanel;

        border.PointerEntered += (s, _) =>
        {
            if (s is Border b)
                b.Background = new SolidColorBrush(Color.Parse(hoverColor));
        };
        border.PointerExited += (s, _) =>
        {
            if (s is Border b)
                b.Background = new SolidColorBrush(Color.Parse(backgroundColor));
        };

        return border;
    }

    private static Border CreateBadge(string text, string color) => new()
    {
        Background = new SolidColorBrush(Color.Parse(color)),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(7, 2),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 10,
            FontWeight = FontWeight.SemiBold
        }
    };

    private void OpenAddWindow()
    {
        var addWindow = new ReminderFormWindow();
        addWindow.Closed += (_, _) => { RefreshCategoryFilter(); LoadReminders(); };
        addWindow.Show();
    }

    private void OnListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_reminderListBox.SelectedItem is Border { Tag: ReminderModel reminder } && !reminder.IsCompleted)
        {
            var editWindow = new ReminderFormWindow(reminder);
            editWindow.Closed += (_, _) => { RefreshCategoryFilter(); LoadReminders(); };
            editWindow.Show();
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            OpenAddWindow();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteSelectedReminder();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            LoadReminders();
            e.Handled = true;
        }
    }

    private async void EditSelectedReminder()
    {
        if (_reminderListBox.SelectedItem is Border { Tag: ReminderModel reminder })
        {
            if (reminder.IsCompleted)
            {
                await UIHelper.ShowMessageAsync(this, "Completed reminders can't be edited. Delete it or create a new one.");
                return;
            }

            var editWindow = new ReminderFormWindow(reminder);
            editWindow.Closed += (_, _) => { RefreshCategoryFilter(); LoadReminders(); };
            editWindow.Show();
        }
        else
        {
            await UIHelper.ShowMessageAsync(this, "Please select a reminder to edit.");
        }
    }

    private async void ReopenSelectedReminder()
    {
        if (_reminderListBox.SelectedItem is Border { Tag: ReminderModel reminder })
        {
            if (!reminder.IsCompleted)
            {
                await UIHelper.ShowMessageAsync(this, "Only completed reminders can be reopened.");
                return;
            }

            try
            {
                var defaultTime = DateTime.Now.AddMinutes(30);
                await App.Service.ReopenReminderAsync(reminder, defaultTime);
                await UIHelper.ShowMessageAsync(this,
                    $"Reminder reopened and scheduled for {ReminderModel.FormatTimeRelative(defaultTime)}.",
                    "Reopened", "↩️");
                LoadReminders();
            }
            catch (Exception ex)
            {
                await UIHelper.ShowMessageAsync(this, $"Error reopening reminder: {ex.Message}", "Error", "❌");
            }
        }
        else
        {
            await UIHelper.ShowMessageAsync(this, "Please select a completed reminder to reopen.");
        }
    }

    private async void MarkSelectedDone()
    {
        if (_reminderListBox.SelectedItem is Border { Tag: ReminderModel reminder })
        {
            if (reminder.IsCompleted)
            {
                await UIHelper.ShowMessageAsync(this, "This reminder is already completed.");
                return;
            }

            try
            {
                await App.Service.MarkReminderAsCompletedAsync(reminder);

                if (reminder.IsRecurring)
                {
                    await UIHelper.ShowMessageAsync(this, $"Reminder completed! Next occurrence has been scheduled ({reminder.Recurrence}).", "Recurring Reminder", "🔁");
                }

                LoadReminders();
            }
            catch (Exception ex)
            {
                await UIHelper.ShowMessageAsync(this, $"Error completing reminder: {ex.Message}", "Error", "❌");
            }
        }
        else
        {
            await UIHelper.ShowMessageAsync(this, "Please select a reminder to mark as done.");
        }
    }

    private async void DeleteSelectedReminder()
    {
        if (_reminderListBox.SelectedItem is Border { Tag: ReminderModel reminder })
        {
            var result = await UIHelper.ShowConfirmAsync(this, $"Are you sure you want to delete:\n\n\"{reminder.Subject}\"?");
            if (result)
            {
                try
                {
                    await App.Service.DeleteReminderAsync(reminder);
                    LoadReminders();
                }
                catch (Exception ex)
                {
                    await UIHelper.ShowMessageAsync(this, $"Error deleting reminder: {ex.Message}", "Error", "❌");
                }
            }
        }
        else
        {
            await UIHelper.ShowMessageAsync(this, "Please select a reminder to delete.");
        }
    }

    private async void ClearCompleted()
    {
        var stats = App.Service.GetStatistics();
        if (stats.Completed == 0)
        {
            await UIHelper.ShowMessageAsync(this, "There are no completed reminders to clear.");
            return;
        }

        var result = await UIHelper.ShowConfirmAsync(this, $"Delete all {stats.Completed} completed reminders?\n\nThis action cannot be undone.");
        if (result)
        {
            try
            {
                await App.Service.DeleteAllCompletedAsync();
                RefreshCategoryFilter();
                LoadReminders();
            }
            catch (Exception ex)
            {
                await UIHelper.ShowMessageAsync(this, $"Error clearing completed reminders: {ex.Message}", "Error", "❌");
            }
        }
    }

    private void RefreshCategoryFilter()
    {
        var previousSelection = _categoryFilter.SelectedItem as string;
        var categories = App.Service.GetCategories();
        var items = new List<string>(categories.Count + 1) { "All Categories" };
        items.AddRange(categories);

        _categoryFilter.ItemsSource = items;

        var idx = previousSelection is not null ? items.IndexOf(previousSelection) : 0;
        _categoryFilter.SelectedIndex = idx >= 0 ? idx : 0;
    }
}
