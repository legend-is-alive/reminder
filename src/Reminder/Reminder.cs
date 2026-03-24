namespace Reminder;

public enum ReminderStatus
{
    Pending,
    Triggered,
    Completed
}

public enum ReminderPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum RecurrenceType
{
    None,
    Daily,
    Weekly,
    Monthly
}

public sealed record ReminderModel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required DateTime Time { get; init; }
    public required string Subject { get; init; }
    public string? Description { get; init; }
    public ReminderStatus Status { get; init; } = ReminderStatus.Pending;
    public ReminderPriority Priority { get; init; } = ReminderPriority.Normal;
    public string? Category { get; init; }
    public RecurrenceType Recurrence { get; init; } = RecurrenceType.None;
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    [JsonIgnore]
    public bool IsCompleted => Status == ReminderStatus.Completed;

    [JsonIgnore]
    public bool IsTriggered => Status == ReminderStatus.Triggered;

    [JsonIgnore]
    public bool IsOverdue => Status == ReminderStatus.Pending && Time < DateTime.Now;

    [JsonIgnore]
    public bool IsRecurring => Recurrence != RecurrenceType.None;

    public bool IsDue(DateTime now) => Status == ReminderStatus.Pending && Time <= now;

    public ReminderModel WithSnooze(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minutes);
        return this with
        {
            Time = DateTime.Now.AddMinutes(minutes),
            Status = ReminderStatus.Pending,
            CompletedAt = null
        };
    }

    public ReminderModel MarkTriggered() => this with
    {
        Status = ReminderStatus.Triggered,
        CompletedAt = null
    };

    public ReminderModel MarkCompleted() => this with
    {
        Status = ReminderStatus.Completed,
        CompletedAt = DateTime.Now
    };

    public ReminderModel Reopen(DateTime time) => this with
    {
        Time = time,
        Status = ReminderStatus.Pending,
        CompletedAt = null
    };

    public ReminderModel? CreateNextRecurrence()
    {
        var nextTime = AdvanceByRecurrence(Time);
        if (nextTime is null)
            return null;

        while (nextTime <= DateTime.Now)
        {
            nextTime = AdvanceByRecurrence(nextTime.Value);
            if (nextTime is null)
                return null;
        }

        return this with
        {
            Id = Guid.NewGuid(),
            Time = nextTime.Value,
            Status = ReminderStatus.Pending,
            CompletedAt = null,
            CreatedAt = DateTime.Now
        };
    }

    private DateTime? AdvanceByRecurrence(DateTime from) => Recurrence switch
    {
        RecurrenceType.Daily => from.AddDays(1),
        RecurrenceType.Weekly => from.AddDays(7),
        RecurrenceType.Monthly => from.AddMonths(1),
        _ => null
    };

    public static string FormatTimeRelative(DateTime time)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var timeStr = time.ToString("HH:mm");

        if (time.Date == today)
            return $"Today at {timeStr}";
        if (time.Date == tomorrow)
            return $"Tomorrow at {timeStr}";
        if (time.Date == today.AddDays(-1))
            return $"Yesterday at {timeStr}";
        if (time.Date > today && time.Date < today.AddDays(7))
            return $"{time:dddd} at {timeStr}";

        return time.ToString("MMM dd, yyyy 'at' HH:mm");
    }

    public static string GetPriorityLabel(ReminderPriority priority) => priority switch
    {
        ReminderPriority.Low => "🔵 Low",
        ReminderPriority.Normal => "🟢 Normal",
        ReminderPriority.High => "🟠 High",
        ReminderPriority.Urgent => "🔴 Urgent",
        _ => "Normal"
    };

    public static string GetPriorityColor(ReminderPriority priority) => priority switch
    {
        ReminderPriority.Low => "#9E9E9E",
        ReminderPriority.Normal => "#2196F3",
        ReminderPriority.High => "#FF9800",
        ReminderPriority.Urgent => "#F44336",
        _ => "#2196F3"
    };

    public static string GetRecurrenceLabel(RecurrenceType recurrence) => recurrence switch
    {
        RecurrenceType.Daily => "🔁 Daily",
        RecurrenceType.Weekly => "🔁 Weekly",
        RecurrenceType.Monthly => "🔁 Monthly",
        _ => "None"
    };

    public static string GetTimeRemainingText(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 2)
            return $"in {(int)timeSpan.TotalDays} days";
        if (timeSpan.TotalDays >= 1)
            return "in 1 day";
        if (timeSpan.TotalHours >= 2)
            return $"in {(int)timeSpan.TotalHours} hours";
        if (timeSpan.TotalHours >= 1)
            return "in 1 hour";
        if (timeSpan.TotalMinutes >= 2)
            return $"in {(int)timeSpan.TotalMinutes} minutes";
        if (timeSpan.TotalMinutes >= 1)
            return "in 1 minute";
        return "less than a minute";
    }
}

public sealed record ReminderStats(int Pending, int Overdue, int DueToday, int Completed, int Total);
