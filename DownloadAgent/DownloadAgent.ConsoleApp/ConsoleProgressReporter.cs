using DownloadAgent.Core;
using System.Collections.Concurrent;

namespace DownloadAgent.ConsoleApp;

public class ConsoleProgressReporter : IProgress<DownloadProgress>
{
    private readonly ConcurrentDictionary<string, DownloadProgress> _activeDownloads = new();
    private readonly object _consoleLock = new();
    private int _lastReportedLineCount = 0;

    public void Report(DownloadProgress value)
    {
        _activeDownloads.AddOrUpdate(
            value.FileUrl,
            value,
            (key, oldValue) => value);

        UpdateConsole();
    }

    private void UpdateConsole()
    {
        lock (_consoleLock)
        {
            // Clear previous progress lines
            for (int i = 0; i < _lastReportedLineCount; i++)
            {
                System.Console.SetCursorPosition(0, System.Console.CursorTop - 1);
                System.Console.Write(new string(' ', System.Console.WindowWidth));
                System.Console.SetCursorPosition(0, System.Console.CursorTop);
            }

        var activeDownloads = _activeDownloads.Values
            .Where(d => d.Status == DownloadStatus.Downloading || d.Status == DownloadStatus.Pending)
            .OrderBy(d => d.FileIndex)
            .ToList();

        var completedCount = _activeDownloads.Values.Count(d => d.Status == DownloadStatus.Completed);
        var failedCount = _activeDownloads.Values.Count(d => d.Status == DownloadStatus.Failed);
        var totalFiles = _activeDownloads.Values.FirstOrDefault()?.TotalFiles ?? 0;

        // Display overall progress
        System.Console.WriteLine($"Overall Progress: {completedCount + failedCount}/{totalFiles} files processed");
        System.Console.WriteLine($"  Completed: {completedCount} | Failed: {failedCount}");
        System.Console.WriteLine();

        // Display active downloads
        if (activeDownloads.Any())
        {
            System.Console.WriteLine("Active Downloads:");
            foreach (var download in activeDownloads)
            {
                var statusIcon = download.Status switch
                {
                    DownloadStatus.Pending => "[ ]",
                    DownloadStatus.Downloading => "[↓]",
                    _ => "   "
                };

                var progressBar = GetProgressBar(download.Percentage, 30);
                var sizeInfo = download.TotalBytes.HasValue
                    ? $"{FormatBytes(download.BytesDownloaded)} / {FormatBytes(download.TotalBytes.Value)}"
                    : FormatBytes(download.BytesDownloaded);

                System.Console.WriteLine(
                    $"  {statusIcon} [{download.FileIndex}/{totalFiles}] {Truncate(download.FileName, 40)}");
                System.Console.WriteLine(
                    $"      {progressBar} {download.Percentage:F1}% | {sizeInfo}");
            }
        }

        _lastReportedLineCount = System.Console.CursorTop;
        }
    }

    public void ShowFinalSummary(List<DownloadResult> results)
    {
        lock (_consoleLock)
        {
            // Clear previous progress
            for (int i = 0; i < _lastReportedLineCount; i++)
            {
                System.Console.SetCursorPosition(0, System.Console.CursorTop - 1);
                System.Console.Write(new string(' ', System.Console.WindowWidth));
                System.Console.SetCursorPosition(0, System.Console.CursorTop);
            }

            var successful = results.Where(r => r.Success).ToList();
            var failed = results.Where(r => !r.Success).ToList();

            System.Console.WriteLine("=== Download Summary ===");
            System.Console.WriteLine($"Total Files: {results.Count}");
            System.Console.WriteLine($"Successful: {successful.Count}");
            System.Console.WriteLine($"Failed: {failed.Count}");
            System.Console.WriteLine();

            if (successful.Any())
            {
                System.Console.WriteLine("Successful Downloads:");
                foreach (var result in successful)
                {
                    var fileName = Path.GetFileName(result.Spec.DestinationPath);
                    System.Console.WriteLine(
                        $"  ✓ {fileName} ({FormatBytes(result.BytesDownloaded)}) - {result.Duration.TotalSeconds:F1}s");
                }
                System.Console.WriteLine();
            }

            if (failed.Any())
            {
                System.Console.WriteLine("Failed Downloads:");
                foreach (var result in failed)
                {
                    var fileName = Path.GetFileName(result.Spec.DestinationPath);
                    System.Console.WriteLine(
                        $"  ✗ {fileName} - {result.ErrorMessage}");
                }
            }
        }
    }

    private static string GetProgressBar(double percentage, int width)
    {
        var filled = (int)(percentage / 100 * width);
        filled = Math.Max(0, Math.Min(width, filled));
        var empty = width - filled;
        return new string('█', filled) + new string('░', empty);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength - 3) + "...";
    }
}

