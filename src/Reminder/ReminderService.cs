namespace Reminder;

public sealed class ReminderService : IAsyncDisposable
{
    private readonly List<ReminderModel> _reminders = [];
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly string _filePath;
    private const int CheckIntervalMs = 3000;
    private bool _disposed;

    public event Action<ReminderModel>? OnReminderTriggered;
    public event Action? OnRemindersChanged;

    public ReminderService()
    {
        _filePath = GetDataFilePath();
        LoadRemindersSync();
        _processingTask = ProcessRemindersAsync(_cts.Token);
    }

    public async Task AddReminderAsync(ReminderModel reminder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        if (string.IsNullOrWhiteSpace(reminder.Subject))
        {
            throw new ArgumentException("Subject cannot be empty", nameof(reminder));
        }

        if (reminder.Time <= DateTime.Now)
        {
            throw new ArgumentException("Reminder time must be in the future", nameof(reminder));
        }

        var normalizedReminder = reminder.Reopen(reminder.Time);

        lock (_lock)
        {
            _reminders.Add(normalizedReminder);
        }

        await SaveRemindersAsync(cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    public async Task SnoozeReminderAsync(ReminderModel reminder, int minutes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutes);

        var snoozedReminder = reminder.WithSnooze(minutes);
        await ReplaceReminderAsync(reminder, snoozedReminder, cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    public List<ReminderModel> GetAllReminders()
    {
        lock (_lock)
        {
            return [.. _reminders.Where(r => !r.IsCompleted).OrderBy(r => r.Time)];
        }
    }

    public List<ReminderModel> GetCompletedReminders()
    {
        lock (_lock)
        {
            return [.. _reminders.Where(r => r.IsCompleted).OrderByDescending(r => r.CompletedAt ?? r.Time)];
        }
    }

    public ReminderStats GetStatistics()
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var pending = _reminders.Count(r => r.Status == ReminderStatus.Pending);
            var overdue = _reminders.Count(r => r.Status == ReminderStatus.Pending && r.Time < now);
            var dueToday = _reminders.Count(r => !r.IsCompleted && r.Time.Date == today);
            var completed = _reminders.Count(r => r.IsCompleted);
            return new ReminderStats(pending, overdue, dueToday, completed, _reminders.Count);
        }
    }

    public List<string> GetCategories()
    {
        lock (_lock)
        {
            return [.. _reminders
                .Select(r => r.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order()];
        }
    }

    public int GetPendingCount()
    {
        lock (_lock)
        {
            return _reminders.Count(r => !r.IsCompleted);
        }
    }

    public async Task UpdateReminderAsync(ReminderModel oldReminder, ReminderModel newReminder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(oldReminder);
        ArgumentNullException.ThrowIfNull(newReminder);

        if (string.IsNullOrWhiteSpace(newReminder.Subject))
        {
            throw new ArgumentException("Subject cannot be empty", nameof(newReminder));
        }

        if (newReminder.Time <= DateTime.Now)
        {
            throw new ArgumentException("Reminder time must be in the future", nameof(newReminder));
        }

        await ReplaceReminderAsync(oldReminder, newReminder.Reopen(newReminder.Time), cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    public async Task MarkReminderAsCompletedAsync(ReminderModel reminder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        if (reminder.IsCompleted)
        {
            return;
        }

        var completedReminder = reminder.MarkCompleted();

        lock (_lock)
        {
            var index = _reminders.IndexOf(reminder);
            if (index < 0)
            {
                throw new InvalidOperationException("Reminder not found");
            }

            _reminders[index] = completedReminder;

            if (reminder.IsRecurring)
            {
                var nextOccurrence = reminder.CreateNextRecurrence();
                if (nextOccurrence is not null)
                {
                    _reminders.Add(nextOccurrence);
                }
            }
        }

        await SaveRemindersAsync(cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    public async Task DeleteReminderAsync(ReminderModel reminder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        lock (_lock)
        {
            _reminders.Remove(reminder);
        }

        await SaveRemindersAsync(cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    public async Task ReopenReminderAsync(ReminderModel reminder, DateTime newTime, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        if (newTime <= DateTime.Now)
        {
            throw new ArgumentException("Reminder time must be in the future", nameof(newTime));
        }

        var reopened = reminder.Reopen(newTime);
        await ReplaceReminderAsync(reminder, reopened, cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    public async Task DeleteAllCompletedAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _reminders.RemoveAll(r => r.IsCompleted);
        }

        await SaveRemindersAsync(cancellationToken);
        OnRemindersChanged?.Invoke();
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessDueRemindersAsync(cancellationToken);
                await Task.Delay(CheckIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error in reminder processing: {ex.Message}");
        }
    }

    private async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        List<ReminderModel> dueReminders;

        lock (_lock)
        {
            var now = DateTime.Now;
            dueReminders = [];

            for (var i = 0; i < _reminders.Count; i++)
            {
                if (!_reminders[i].IsDue(now))
                {
                    continue;
                }

                var triggeredReminder = _reminders[i].MarkTriggered();
                _reminders[i] = triggeredReminder;
                dueReminders.Add(triggeredReminder);
            }
        }

        if (dueReminders.Count == 0)
            return;

        await SaveRemindersAsync(cancellationToken);
        OnRemindersChanged?.Invoke();

        foreach (var reminder in dueReminders)
        {
            try
            {
                OnReminderTriggered?.Invoke(reminder);
            }
            catch (Exception ex)
            {
                ErrorDialog.Show($"Error triggering reminder '{reminder.Subject}': {ex.Message}");
            }
        }
    }

    private async Task SaveRemindersAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            List<ReminderModel> remindersList;
            lock (_lock)
            {
                remindersList = [.._reminders];
            }

            var tempPath = _filePath + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, remindersList, ReminderJsonContext.Default.ListReminderModel, cancellationToken);
            }

            File.Move(tempPath, _filePath, overwrite: true);

        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error saving reminders: {ex.Message}");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private void LoadRemindersSync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            using var stream = File.OpenRead(_filePath);
            var loadedReminders = JsonSerializer.Deserialize(stream, ReminderJsonContext.Default.ListReminderModel);

            if (loadedReminders is null)
            {
                return;
            }

            lock (_lock)
            {
                _reminders.AddRange(loadedReminders);
            }

        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error loading reminders: {ex.Message}");
        }
    }

    private static string GetDataFilePath()
    {
        const string fileName = "reminders.json";
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Reminder");
        Directory.CreateDirectory(appDataDir);
        var newPath = Path.Combine(appDataDir, fileName);

        if (!File.Exists(newPath) && File.Exists(fileName))
        {
            try
            {
                File.Copy(fileName, newPath);
            }
            catch (Exception ex)
            {
                ErrorDialog.Show($"Error migrating reminders file: {ex.Message}");
                return fileName;
            }
        }

        return newPath;
    }

    private async Task ReplaceReminderAsync(ReminderModel existingReminder, ReminderModel updatedReminder, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var index = _reminders.IndexOf(existingReminder);
            if (index < 0)
            {
                throw new InvalidOperationException("Reminder not found");
            }

            _reminders[index] = updatedReminder;
        }

        await SaveRemindersAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await _cts.CancelAsync();

        try
        {
            await _processingTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error during shutdown: {ex.Message}");
        }

        _cts.Dispose();
        _fileLock.Dispose();
    }
}
