using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class FileService
{
    // Get the path to the application data folder
    public static string GetApplicationDataFolder()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataFolder, "AcesysConversion");
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        return appFolder;
    }

    // Save XML content to a file
    public static async Task SaveXmlFileAsync(string fileName, string xmlContent)
    {
        string folderPath = GetApplicationDataFolder();
        string filePath = Path.Combine(folderPath, fileName);
        await File.WriteAllTextAsync(filePath, xmlContent);
    }

    // Retrieve XML content from a file
    public static async Task<string?> RetrieveXmlFileAsync(string fileName)
    {
        string folderPath = GetApplicationDataFolder();
        string filePath = Path.Combine(folderPath, fileName);
        if (File.Exists(filePath))
        {
            return await File.ReadAllTextAsync(filePath);
        }
        return null;
    }

    // Optionally delete a file
    public static void DeleteXmlFile(string fileName)
    {
        string folderPath = GetApplicationDataFolder();
        string filePath = Path.Combine(folderPath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}