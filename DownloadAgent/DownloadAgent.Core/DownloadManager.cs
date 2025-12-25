using System.Net;

namespace DownloadAgent.Core;

public class DownloadManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrency;
    private const int BufferSize = 8192; // 8KB buffer

    public DownloadManager(int maxConcurrency = 3)
    {
        _maxConcurrency = maxConcurrency;
        _httpClient = new HttpClient();
    }

    public async Task<List<DownloadResult>> DownloadBatchAsync(
        DownloadBatch batch,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var downloadSpecs = batch.DownloadSpecs.ToList();
        var totalFiles = downloadSpecs.Count;
        var results = new List<DownloadResult>();
        var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

        var tasks = downloadSpecs.Select(async (spec, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await DownloadFileAsync(
                    spec,
                    index + 1,
                    totalFiles,
                    progress,
                    cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var downloadResults = await Task.WhenAll(tasks);
        return downloadResults.ToList();
    }

    private async Task<DownloadResult> DownloadFileAsync(
        DownloadSpec spec,
        int fileIndex,
        int totalFiles,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new DownloadResult
        {
            Spec = spec,
            Success = false
        };

        try
        {
            // Report pending status
            ReportProgress(progress, new DownloadProgress
            {
                FileUrl = spec.Url,
                FileName = Path.GetFileName(spec.DestinationPath),
                Status = DownloadStatus.Pending,
                FileIndex = fileIndex,
                TotalFiles = totalFiles
            });

            // Ensure destination directory exists
            var destinationDir = Path.GetDirectoryName(spec.DestinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Get response with headers only first
            using var response = await _httpClient.GetAsync(
                spec.Url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            var bytesDownloaded = 0L;

            // Report downloading status
            ReportProgress(progress, new DownloadProgress
            {
                FileUrl = spec.Url,
                FileName = Path.GetFileName(spec.DestinationPath),
                Status = DownloadStatus.Downloading,
                FileIndex = fileIndex,
                TotalFiles = totalFiles,
                TotalBytes = totalBytes
            });

            // Stream download with progress tracking
            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(
                spec.DestinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                true);

            var buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                bytesDownloaded += bytesRead;

                // Report progress periodically (every buffer read)
                ReportProgress(progress, new DownloadProgress
                {
                    FileUrl = spec.Url,
                    FileName = Path.GetFileName(spec.DestinationPath),
                    Status = DownloadStatus.Downloading,
                    BytesDownloaded = bytesDownloaded,
                    TotalBytes = totalBytes,
                    FileIndex = fileIndex,
                    TotalFiles = totalFiles
                });
            }

            var duration = DateTime.UtcNow - startTime;
            result.Success = true;
            result.BytesDownloaded = bytesDownloaded;
            result.Duration = duration;

            // Report completed status
            ReportProgress(progress, new DownloadProgress
            {
                FileUrl = spec.Url,
                FileName = Path.GetFileName(spec.DestinationPath),
                Status = DownloadStatus.Completed,
                BytesDownloaded = bytesDownloaded,
                TotalBytes = totalBytes,
                FileIndex = fileIndex,
                TotalFiles = totalFiles
            });
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = duration;

            // Report failed status
            ReportProgress(progress, new DownloadProgress
            {
                FileUrl = spec.Url,
                FileName = Path.GetFileName(spec.DestinationPath),
                Status = DownloadStatus.Failed,
                FileIndex = fileIndex,
                TotalFiles = totalFiles,
                ErrorMessage = ex.Message
            });
        }

        return result;
    }

    private void ReportProgress(IProgress<DownloadProgress>? progress, DownloadProgress downloadProgress)
    {
        progress?.Report(downloadProgress);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
