using CropVideoExtractAnimatedImage.Extensions;
using CropVideoExtractAnimatedImage.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xabe.FFmpeg;

namespace CropVideoExtractAnimatedImage.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("CropVideoExtractImageAndGenerateGif")]
        public async Task<IActionResult> MergeImagesAsync([FromBody] Base64FileRequest request)
        {
            var validVideoFormats = new List<string>() { "mp4", "mpeg", "avi" };

            try
            {
                var uploadFolderPath = _configuration.GetSection("UploadFolderPath").Value;
                var extension = request.FileName.Substring(request.FileName.LastIndexOf("."),
                    request.FileName.Length - request.FileName.LastIndexOf(".")).Replace(".", "");

                #region Validations

                if (string.IsNullOrEmpty(uploadFolderPath))
                    throw new Exception("UploadFolderPath not configured.");

                if (uploadFolderPath.ToUpper() != "%TEMP%")
                {
                    try
                    {
                        Path.GetFullPath(uploadFolderPath);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("UploadFolderPath is invalid.");
                    }
                }
                else
                {
                    uploadFolderPath = Path.GetTempPath();
                }


                if (string.IsNullOrEmpty(request.Base64Data) || !request.Base64Data.IsBase64Encoded())
                    throw new Exception("Video base 64 data not provided or is invalid base 64 string.");
                
                if (!validVideoFormats.Contains(extension))
                    throw new Exception($"Invalid format format, video formats alowed are {string.Join(", ", validVideoFormats)}.");

                #endregion

                if (!uploadFolderPath.EndsWith(Path.DirectorySeparatorChar))
                    uploadFolderPath = $"{uploadFolderPath}{Path.DirectorySeparatorChar}";

                if (!Directory.Exists(uploadFolderPath))
                    Directory.CreateDirectory(uploadFolderPath);

                var imageFileName = $"{request.FileName.Substring(0, request.FileName.LastIndexOf("."))} {DateTime.Now.ToString("O")}.png".NormalizeFileName();
                var gitFileName = $"{request.FileName.Substring(0, request.FileName.LastIndexOf("."))} {DateTime.Now.ToString("O")}.gif".NormalizeFileName();
                var cropedVideoFileName = $"{request.FileName.Substring(0, request.FileName.LastIndexOf("."))} {DateTime.Now.ToString("O")}.mp4".NormalizeFileName();


                var originalVideoPath = $"{Path.GetTempPath()}{Guid.NewGuid()}.{extension}";
                request.Base64Data.SaveBase64AsFile(originalVideoPath);

                FFmpeg.SetExecutablesPath(FFmpegDownloaderBase.FFmpegPath);
                var mediaInfo = await FFmpeg.GetMediaInfo(originalVideoPath);

                #region Extract video frame imagens and turn it into Single image and GIF

                var videoStream1 = mediaInfo.VideoStreams.First()?.SetCodec(VideoCodec.png);
                var ffmpegConversions = FFmpeg.Conversions.New().AddStream(videoStream1);
                var imagesPath = new List<string>();
                for (var frameNo = 1; frameNo < 100; frameNo++)
                {
                    var imagePath = $"{Path.GetTempPath()}{Guid.NewGuid()}.png";
                    await ffmpegConversions.ExtractNthFrame(frameNo, s => imagePath)
                        .Start();

                    imagesPath.Add(imagePath);
                }

                using (var image = Image.Load<Rgba32>(imagesPath.Last()))
                {
                    image.SaveAsPng($"{uploadFolderPath}{imageFileName}");
                }

                // Delay between frames in (1/100) of a second.
                var frameDelay = 10;

                using (var baseImage = Image.Load<Rgba32>(imagesPath.First()))
                using (var gifImage = new Image<Rgba32>(baseImage.Width, baseImage.Height))
                {
                    // Set animation loop repeat count to 5.
                    var gifMetaData = gifImage.Metadata.GetGifMetadata();
                    gifMetaData.RepeatCount = 5;

                    // Set the delay until the next image is displayed.
                    GifFrameMetadata metadata = gifImage.Frames.RootFrame.Metadata.GetGifMetadata();
                    metadata.FrameDelay = frameDelay;

                    foreach (var imagePath in imagesPath)
                    {
                        using (var frameImage = Image.Load<Rgba32>(imagePath))
                        {
                            // Set the delay until the next image is displayed.
                            metadata = frameImage.Frames.RootFrame.Metadata.GetGifMetadata();
                            metadata.FrameDelay = frameDelay;

                            // Add the frame image to the gif.
                            gifImage.Frames.AddFrame(frameImage.Frames.RootFrame);
                        }
                    }

                    gifImage.SaveAsGif($"{uploadFolderPath}{gitFileName}");
                }

                #endregion

                #region Crop video

                var partVideoDuration = mediaInfo.Duration / 2; //Cut video in half

                await FFmpeg.Conversions.New()
                    .AddStream(mediaInfo.Streams)
                    // -ss (start position), -t (duration)
                    .AddParameter($"-ss {TimeSpan.FromSeconds(1)} -t {partVideoDuration}")
                    .SetOutput($"{uploadFolderPath}{cropedVideoFileName}")
                    .Start();

                #endregion


                #region Clear temp files
                
                if (System.IO.File.Exists(originalVideoPath))
                    System.IO.File.Delete(originalVideoPath);

                foreach (var imagePath in imagesPath)
                {
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                #endregion

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest("Could not merge images");
            }
        }
    }
}
