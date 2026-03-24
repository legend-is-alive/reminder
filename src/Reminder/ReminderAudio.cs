using NetCoreAudio;

namespace Reminder;

public static class ReminderAudio
{
    private static readonly string SoundFilePath = GetSoundFilePath();
    private static readonly Lazy<string> ToneFilePath = new(CreateToneFilePath);

    public static void PlayNotificationSound()
    {
        _ = PlayNotificationSoundAsync();
    }

    private static async Task PlayNotificationSoundAsync()
    {
        try
        {
            var player = new Player();

            if (File.Exists(SoundFilePath))
            {
                await player.Play(SoundFilePath);
            }
            else
            {
                await player.Play(ToneFilePath.Value);
            }
        }
        catch (Exception ex)
        {
            ErrorDialog.Show($"Error playing reminder sound: {ex.Message}");
        }
    }

    private static string GetSoundFilePath()
    {
        var baseDirectory = AppContext.BaseDirectory;

        return Path.Combine(baseDirectory, "Sounds", "notify.wav");
    }

    private static string CreateToneFilePath()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "reminder-notification.wav");

        if (!File.Exists(filePath))
        {
            File.WriteAllBytes(filePath, CreateToneWave());
        }

        return filePath;
    }

    private static byte[] CreateToneWave()
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        const int sampleRate = 24_000;
        const int durationMs = 1200;
        const double fundamentalFrequency = 1318.51;
        const double overtoneFrequency = 2637.02;
        const double amplitude = 0.3;

        var sampleCount = sampleRate * durationMs / 1000;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var byteRate = sampleRate * blockAlign;
        var dataSize = sampleCount * blockAlign;

        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        for (var i = 0; i < sampleCount; i++)
        {
            var time = (double)i / sampleRate;
            var decay = Math.Exp(-3.2 * time);
            var strike = Math.Min(1d, time * 40);
            var tone =
                (Math.Sin(2 * Math.PI * fundamentalFrequency * time) * 0.7) +
                (Math.Sin(2 * Math.PI * overtoneFrequency * time) * 0.3);

            var sample = (short)(tone * strike * decay * short.MaxValue * amplitude);
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }
}