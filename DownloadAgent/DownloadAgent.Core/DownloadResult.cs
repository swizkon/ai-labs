namespace DownloadAgent.Core;

public class DownloadResult
{
    public DownloadSpec Spec { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long BytesDownloaded { get; set; }
    public TimeSpan Duration { get; set; }
}

