public class DownloadManager
{
    public void DownloadFile(string url, string destinationPath)
    {
        var client = new WebClient();
        client.DownloadFile(url, destinationPath);
    }
}