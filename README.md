# Reminder

A lightweight desktop reminder application built with [Avalonia UI](https://avaloniaui.net/) and .NET 10. It lives in your system tray, keeps track of your tasks, and pops up notifications with sound when reminders are due.

## Features

- **System tray integration** — runs quietly in the background with a tray icon and context menu
- **Single-instance window** — opening the reminder list always reuses the existing window
- **Create, edit & delete reminders** — full form with subject, description, date, time (24h), and more
- **Reopen completed reminders** — reschedule finished reminders with one click
- **Priority levels** — Low, Normal, High, Urgent with color-coded indicators
- **Categories** — organize reminders with custom tags (autocomplete from existing categories)
- **Recurring reminders** — Daily, Weekly, or Monthly; next occurrence is created automatically on completion
- **Popup notifications** — toast-style popup in the corner of the screen when a reminder is due
- **Multiple snooze options** — snooze for 5, 15, 30, or 60 minutes from the popup
- **Notification sound** — plays a sound when a reminder fires (custom `.wav` or built-in fallback tone)
- **Search & filter** — search by subject, description, or category; filter by priority and category
- **Sort options** — sort by time, priority, or name
- **Statistics** — see pending, overdue, due today, and completed counts at a glance
- **Mark done from list** — complete reminders directly without opening the edit form
- **Bulk clear completed** — delete all completed reminders in one click
- **Keyboard shortcuts** — `Ctrl+N` (new), `Delete` (delete selected), `F5` (refresh), with tooltips on buttons
- **Double-click to edit** — double-click any reminder in the list to open the edit form
- **24-hour time format** — all times displayed in HH:mm format
- **Persistent storage** — reminders saved to a local JSON file in `%LocalAppData%\Reminder\`
- **Pending count in tray tooltip** — tray icon shows how many reminders are pending

## Project Structure

```
src/Reminder/
├── App.axaml / App.axaml.cs    # Application entry and lifecycle
├── Program.cs                   # Avalonia app builder
├── Reminder.cs                  # Data model, enums (status, priority, recurrence)
├── ReminderService.cs           # Core service: CRUD, scheduling, persistence
├── ReminderFormWindow.cs        # Create / edit reminder form
├── ReminderListWindow.cs        # Main list with search, filter, sort, stats
├── ReminderPopup.cs             # Notification popup with snooze options
├── HiddenWindow.cs              # System tray host window
├── ReminderAudio.cs             # Sound playback (custom wav or generated tone)
├── ReminderJsonContext.cs       # System.Text.Json source generator context
├── UIHelper.cs                  # Dialog helpers and UI utilities
├── ErrorDialog.cs               # Error display window
├── RelayCommand.cs              # ICommand implementation for tray menu
├── AppConstants.cs              # Shared constants (icon path)
├── GlobalUsings.cs              # Global using directives
├── Icons/                       # Application icons
└── Sounds/                      # Notification sound files
```

## Tech Stack

| Component | Technology |
|---|---|
| UI Framework | [Avalonia UI 11](https://avaloniaui.net/) with Fluent theme |
| Runtime | .NET 10, C# 14 |
| System Tray | [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) |
| Audio | [NetCoreAudio](https://github.com/nickvdyck/NetCoreAudio) |
| Serialization | System.Text.Json (source-generated) |

## Getting Started

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Clone

```bash
git clone https://github.com/legend-is-alive/reminder.git
cd reminder
```

### Build

```bash
dotnet build src/Reminder/Reminder.csproj
```

### Run

```bash
dotnet run --project src/Reminder/Reminder.csproj
```

The app starts minimized to the system tray. Right-click the tray icon to access the menu.

## How to Use

### Getting Started

```
1. Run the app              2. Find the tray icon       3. Right-click for menu
   dotnet run        →      [🔔 in system tray]   →     ┌──────────────────┐
                                                         │ 📋 View Reminders│
                                                         │ ➕ Add Reminder  │
                                                         │ ──────────────── │
                                                         │ 🚪 Exit          │
                                                         └──────────────────┘
```

### Creating a Reminder

```
┌─────────────────────────────┐
│ ➕ Create New Reminder      │
│ ─────────────────────────── │
│ 📝 Subject:    [Meeting]    │
│ 📄 Description:[With team]  │
│ ─────────────────────────── │
│ 📅 Date:       [2025-01-20] │
│ 🕐 Time:       [14:30]      │
│ ─────────────────────────── │
│ ⚡ Priority:   [🟢 Normal]  │
│ 🏷️ Category:   [Work]       │
│ 🔁 Repeat:     [Weekly]     │
│ ─────────────────────────── │
│        [Cancel] [💾 Create] │
└─────────────────────────────┘
```

### Managing Reminders

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 📋 Your Reminders                                                       │
│ Manage, organize, and track all your reminders — Ctrl+N to add, Del to  │
│ remove                                                                  │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ 🔍 Search reminders...                                             │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│ [➕ Add] [✏️ Edit] [✓ Done] [↩️ Reopen] [🗑 Delete] [🔄 Refresh] [🧹]  │
│ Priority: [All ▼]  Category: [All ▼]  Sort: [Time ↑ ▼]  ☐ Show done   │
│                                                                         │
│ 📌 3 pending · ⚠️ 1 overdue · 📅 2 due today · ✅ 5 done · 📊 8 total  │
│                                                                         │
│ ┌─┬───────────────────────────────────────────────────────────────────┐ │
│ │▊│ Team Meeting                    [High] [🔁 Weekly]                │ │
│ │▊│ Discuss Q1 milestones                                             │ │
│ │▊│ ⏰ Today at 14:30  (in 2 hours)                      🏷️ Work     │ │
│ ├─┼───────────────────────────────────────────────────────────────────┤ │
│ │▊│ Dentist Appointment                                               │ │
│ │▊│ ⏰ Tomorrow at 10:00  (in 1 day)                     🏷️ Health   │ │
│ ├─┼───────────────────────────────────────────────────────────────────┤ │
│ │▊│ Call Mom                                               [Overdue]  │ │
│ │▊│ ⏰ Yesterday at 18:00                                 🏷️ Personal │ │
│ └─┴───────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
  Keyboard: Ctrl+N = New  |  Delete = Remove  |  F5 = Refresh
            Double-click a reminder to edit it
            Hover over buttons to see shortcut hints
```

### When a Reminder Triggers

```
┌─────────────────────────────────────┐
│ 🔔 Team Meeting              [High] │
│    Today at 14:30         🔁 Weekly  │
│ ─────────────────────────────────── │
│ Discuss Q1 milestones                │
│                                      │
│ Snooze for:                          │
│ [⏰ 5m] [⏰ 15m] [⏰ 30m] [⏰ 1h]   │
│                                      │
│              [Dismiss] [✓ Mark Done] │
└─────────────────────────────────────┘
```

### Common Workflows

| Workflow | Steps |
|---|---|
| **Set a reminder** | Tray → Add Reminder → fill form → Create |
| **Recurring task** | Add Reminder → set Repeat to Daily/Weekly/Monthly → Create |
| **Snooze** | Popup appears → pick 5m / 15m / 30m / 1h |
| **Complete** | Popup → Mark Done, or List → select → ✓ Done |
| **Reopen** | List → show completed → select → ↩️ Reopen |
| **Edit** | List → select → Edit, or double-click |
| **Search** | List → type in search box → results filter live |
| **Filter by category** | List → Category dropdown → pick a category |
| **Clean up** | List → 🧹 Clear Done → confirm |

## Data Storage

Reminders are persisted as JSON in:

```
%LocalAppData%\Reminder\reminders.json
```

The file is written atomically (write to `.tmp`, then move) to prevent corruption.

## License

This project is licensed under the [MIT License](LICENSE).
