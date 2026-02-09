namespace System.IO;

public static class FileValidationExtensions
{
    public static bool ValidateFileExists(this string? filePath, string errorMessage = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            throw new Exception(errorMessage ?? $"{filePath}，文件不存在!");
        }
        return true;
    }
    public static void CreateDirectory(this string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(filePath);
        }
    }
}