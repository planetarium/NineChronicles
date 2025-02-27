using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class FileHelper
    {
        public static void WriteAllText(string fileName, string text)
        {
            var path = Platform.GetPersistentDataPath(fileName);
            File.WriteAllText(path, text);
        }

        public static void AppendAllText(string fileName, string text)
        {
            var path = Platform.GetPersistentDataPath(fileName);
            File.AppendAllText(path, text);
        }
        public static void WriteAllText(string directoryName, string fileName, string text)
        {
            var dirPath = Platform.GetPersistentDataDirectoryPath(directoryName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            var path = Path.Join(dirPath, fileName);
            File.WriteAllText(path, text);
        }

        public static async Task<string> ReadAllTextAsync(string directoryName, string fileName)
        {
            var dirPath = Platform.GetPersistentDataDirectoryPath(directoryName);
            var path = Path.Join(dirPath, fileName);
            return await File.ReadAllTextAsync(path);
        }
    }
}
