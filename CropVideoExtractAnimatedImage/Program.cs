using CropVideoExtractAnimatedImage;
using Xabe.FFmpeg.Downloader;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Services

builder.Services.AddScoped<IFFmpegDownloader, FFmpegDownloaderBase>();

#endregion

var app = builder.Build();

using (var serviceScope = app.Services.CreateScope())
{
    if (!Directory.Exists(FFmpegDownloaderBase.FFmpegPath))
        Directory.CreateDirectory(FFmpegDownloaderBase.FFmpegPath);

    var files = Directory.GetFiles(FFmpegDownloaderBase.FFmpegPath, "ffmpeg.*");

    if (files.Length == 0)
    {
        var ffmpegDownloader = serviceScope.ServiceProvider.GetService<IFFmpegDownloader>();
        await ffmpegDownloader.GetLatestVersion(FFmpegDownloaderBase.FFmpegPath);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();