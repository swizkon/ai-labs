using DownloadAgent.Core;

namespace DownloadAgent.ConsoleApp;

class Program
{
    public static async Task Main(string[] args)
    {

        var downloadSpecs = new List<DownloadSpec>
        {
            // Example downloads - replace with your actual URLs and paths
            new DownloadSpec("http://localhost:5057/textfile?size=3000", "downloads/file1.txt"),
            new DownloadSpec("http://localhost:5057/textfile?size=30000", "downloads/file2.txt"),
            new DownloadSpec("http://localhost:5057/textfile?size=300000", "downloads/file3.txt"),
            new DownloadSpec("http://localhost:5057/textfile?size=300000", "downloads/file4.txt"),
            new DownloadSpec("http://localhost:5057/textfile?size=300000", "downloads/file5.txt")
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