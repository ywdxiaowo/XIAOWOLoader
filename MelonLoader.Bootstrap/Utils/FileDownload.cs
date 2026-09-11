namespace MelonLoader.Bootstrap.RuntimeHandlers.Dotnet;

internal class FileDownload
{
    public string? URL { get; private set; } = null!;

    internal FileDownload(string url)
    {
        URL = url;
    }

    public (bool, string?) Attempt(string filePath)
        => AttemptAsync(filePath).GetAwaiter().GetResult();

    private async Task<(bool, string?)> AttemptAsync(string filePath)
    {
        var filePathDir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(filePathDir))
            Directory.CreateDirectory(filePathDir!);

        var partPath = filePath + ".part";
        var urls = LoaderConfig.Current.Network.GetDownloadUrls(URL!);
        var retryCount = Math.Clamp(LoaderConfig.Current.Network.RetryCount, 0, 5);
        string? lastError = null;

        for (var urlIndex = 0; urlIndex < urls.Length; urlIndex++)
        {
            var url = urls[urlIndex];
            var retriesForUrl = urlIndex == 0 && urls.Length > 1 ? 0 : retryCount;
            for (var attempt = 0; attempt <= retriesForUrl; attempt++)
            {
                if (File.Exists(partPath))
                    File.Delete(partPath);

                try
                {
                    using var handler = new SocketsHttpHandler
                    {
                        ConnectTimeout = TimeSpan.FromSeconds(Math.Clamp(
                            LoaderConfig.Current.Network.ConnectTimeoutSeconds,
                            1,
                            60))
                    };

                    if (!string.IsNullOrWhiteSpace(LoaderConfig.Current.Network.ProxyUrl))
                    {
                        handler.Proxy = new System.Net.WebProxy(LoaderConfig.Current.Network.ProxyUrl);
                        handler.UseProxy = true;
                    }

                    using var http = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(Math.Clamp(
                            LoaderConfig.Current.Network.DownloadTimeoutSeconds,
                            10,
                            3600))
                    };
                    http.DefaultRequestHeaders.Add("User-Agent", "MelonLoader");

                    Core.Logger.Msg($"Downloading from: {url}");
                    using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (!resp.IsSuccessStatusCode)
                    {
                        lastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
                        continue;
                    }

                    var totalBytes = resp.Content.Headers.ContentLength;
                    await using var contentStream = await resp.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(
                        partPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 81920,
                        useAsync: true);

                    var buffer = new byte[81920];
                    long totalBytesRead = 0;
                    var lastProgress = -1;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        if (totalBytes is > 0)
                        {
                            var progress = (int)((totalBytesRead * 100L) / totalBytes.Value);
                            if (progress != lastProgress)
                            {
                                lastProgress = progress;
                                Core.Logger.Msg(progress + "%");
                            }
                        }
                    }

                    await fileStream.FlushAsync();
                    if (totalBytes.HasValue && totalBytesRead != totalBytes.Value)
                    {
                        lastError = $"Incomplete download: expected {totalBytes.Value} bytes, received {totalBytesRead} bytes";
                        continue;
                    }

                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    File.Move(partPath, filePath);
                    return (true, null);
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
                finally
                {
                    if (File.Exists(partPath))
                        File.Delete(partPath);
                }

                if (attempt < retriesForUrl)
                    await Task.Delay(TimeSpan.FromSeconds(1 << attempt));
            }
        }

        return (false, lastError);
    }
}
