namespace Reminder;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<ReminderModel>))]
internal sealed partial class ReminderJsonContext : JsonSerializerContext;
