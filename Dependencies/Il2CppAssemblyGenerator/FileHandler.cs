using System;
using System.IO;
using System.IO.Compression;

namespace MelonLoader.Il2CppAssemblyGenerator
{
    internal static class FileHandler
    {
        internal static bool Download(string url, string destination)
        {
            if (string.IsNullOrEmpty(url))
            {
                Core.Logger.Error($"url cannot be Null or Empty!");
                return false;
            }

            if (string.IsNullOrEmpty(destination))
            {
                Core.Logger.Error($"destination cannot be Null or Empty!");
                return false;
            }

            var partPath = destination + ".part";
            var urls = LoaderConfig.Current.Network.GetDownloadUrls(url);
            var retryCount = Math.Max(0, Math.Min(LoaderConfig.Current.Network.RetryCount, 5));
            Exception lastException = null;

            for (var urlIndex = 0; urlIndex < urls.Length; urlIndex++)
            {
                var downloadUrl = urls[urlIndex];
                var retriesForUrl = urlIndex == 0 && urls.Length > 1 ? 0 : retryCount;
                for (var attempt = 0; attempt <= retriesForUrl; attempt++)
                {
                    if (File.Exists(partPath))
                        File.Delete(partPath);

                    Core.Logger.Msg($"Downloading {downloadUrl} to {destination}");
                    try
                    {
                        Core.webClient.DownloadFile(downloadUrl, partPath);
                        if (File.Exists(destination))
                            File.Delete(destination);
                        File.Move(partPath, destination);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        if (File.Exists(partPath))
                            File.Delete(partPath);
                    }

                    if (attempt < retriesForUrl)
                        System.Threading.Thread.Sleep(1000 << attempt);
                }
            }

            if (lastException != null)
                Core.Logger.Error(lastException.ToString());
            return false;
        }

        internal static bool Process(string filepath, string destination, string targetName = null)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Core.Logger.Error($"filepath cannot be Null or Empty!");
                return false;
            }

            if (string.IsNullOrEmpty(destination))
            {
                Core.Logger.Error($"destination cannot be Null or Empty!");
                return false;
            }

            if (filepath.Equals(destination))
                return true;

            if (!File.Exists(filepath))
            {
                Core.Logger.Error($"{filepath} does not Exist!");
                return false;
            }

            if (Path.HasExtension(destination))
            {
                if (File.Exists(destination))
                    File.Delete(destination);
            }
            else
            {
                if (Directory.Exists(destination))
                {
                    Core.Logger.Msg($"Cleaning {destination}");
                    foreach (var entry in Directory.EnumerateFileSystemEntries(destination))
                    {
                        if (Directory.Exists(entry))
                            Directory.Delete(entry, true);
                        else
                            File.Delete(entry);
                    }
                }
                else
                {
                    Core.Logger.Msg($"Creating Directory {destination}");
                    Directory.CreateDirectory(destination);
                }
            }

            string filename = Path.GetFileName(filepath);
            if (!filename.EndsWith(".zip"))
            {
                Core.Logger.Msg($"Moving {filepath} to {destination}");
                
                if (!string.IsNullOrEmpty(targetName))
                    destination = Path.Combine(destination, targetName);
                
                File.Move(filepath, destination);
                return true;
            }

            Core.Logger.Msg($"Extracting {filepath} to {destination}");
            try { ZipFile.ExtractToDirectory(filepath, destination); }
            catch (Exception ex)
            {
                Core.Logger.Error(ex.ToString());

                if (File.Exists(filepath))
                    File.Delete(filepath);

                if (Directory.Exists(destination))
                {
                    foreach (var entry in Directory.EnumerateFileSystemEntries(destination))
                    {
                        if (Directory.Exists(entry))
                            Directory.Delete(entry, true);
                        else
                            File.Delete(entry);
                    }
                }

                return false;
            }

            if (File.Exists(filepath))
                File.Delete(filepath);

            return true;
        }
    }
}
