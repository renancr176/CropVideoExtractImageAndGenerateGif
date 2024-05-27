using System.ComponentModel.DataAnnotations;

namespace CropVideoExtractAnimatedImage.Models.Requests;

public class Base64FileRequest
{
    [Required]
    public string FileName { get; set; }
    [Required]
    public string MimeType { get; set; }
    [Required]
    public string Base64Data { get; set; }
}