using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace CropVideoExtractAnimatedImage;

public class FFmpegDownloaderBase : FFmpegDownloader, IFFmpegDownloader
{
    public static string FFmpegPath = $"{AppDomain.CurrentDomain.BaseDirectory}FFmpeg{Path.DirectorySeparatorChar}";


    public async Task GetLatestVersion(string path, IProgress<ProgressInfo> progress = null, int retries = 3)
    {
        var count = 1;
        var sucess = false;
        do
        {
            try
            {
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, path, progress);
                sucess = true;
            }
            catch (Exception e)
            {
                count++;
            }
        } while (!sucess && count <= retries);

        if (!sucess)
            throw new Exception("Error when trying to download FFmpeg. Download attempts exceeded."); 
    }
}