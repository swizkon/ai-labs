using DownloadAgent.Core;

namespace DownloadAgent.ConsoleApp;

class Program
{
    public static async Task Main(string[] args)
    {

        var downloadSpecs = new List<DownloadSpec>
        {
            // Example downloads - replace with your actual URLs and paths
            new DownloadSpec("https://example.com/file1.zip", "downloads/file1.zip"),
            new DownloadSpec("https://example.com/file2.zip", "downloads/file2.zip"),
            new DownloadSpec("https://example.com/file3.zip", "downloads/file3.zip")
        };

        var batch = new DownloadBatch(downloadSpecs);
        var progressReporter = new ConsoleProgressReporter();

        Console.WriteLine("Starting download batch...");
        Console.WriteLine();

        using var downloadManager = new DownloadManager(maxConcurrency: 3);
        var results = await downloadManager.DownloadBatchAsync(batch, progressReporter);

        Console.WriteLine();
        progressReporter.ShowFinalSummary(results);
    }
}