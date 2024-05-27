using System.Text;

namespace CropVideoExtractAnimatedImage.Extensions;

public static class StringExtensions
{
    public static string Base64Encode(this string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(this string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public static bool IsBase64Encoded(this string base64EncodedData)
    {
        try
        {
            var test = Base64Decode(base64EncodedData);
            return true;
        }
        catch (Exception)
        {
        }

        return false;
    }

    public static void SaveBase64AsFile(this string content, string filename)
    {
        File.WriteAllBytes(filename, Convert.FromBase64String(content));
    }

    public static string? ToBase64(this string filePath)
    {
        if (File.Exists(filePath))
        {
            return Convert.ToBase64String(File.ReadAllBytes(filePath));
        }

        return null;
    }

    public static string NormalizeFileName(this string fileName)
    {
        // Not allowed character - Replacement
        var notAllowedCharacters = new Dictionary<string, string>()
        {
            {"\\", "-"},
            {"/", "-"},
            {":", "-"},
            {"*", ""},
            {"?", ""},
            {"\"", ""},
            {"<", ""},
            {">", ""},
            {"|", ""},
        };

        foreach (var item in notAllowedCharacters)
        {
            fileName = fileName.Replace(item.Key, item.Value);
        }

        return fileName;
    }
}