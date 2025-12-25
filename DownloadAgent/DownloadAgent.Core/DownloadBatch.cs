namespace DownloadAgent.Core;

public record DownloadBatch(IEnumerable<DownloadSpec> DownloadSpecs);