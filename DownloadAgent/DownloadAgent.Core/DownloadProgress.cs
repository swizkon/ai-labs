namespace DownloadAgent.Core;

public class DownloadProgress
{
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long? TotalBytes { get; set; }
    public double Percentage => TotalBytes.HasValue && TotalBytes.Value > 0
        ? (double)BytesDownloaded / TotalBytes.Value * 100
        : 0;
    public DownloadStatus Status { get; set; }
    public int FileIndex { get; set; }
    public int TotalFiles { get; set; }
    public string? ErrorMessage { get; set; }
}

