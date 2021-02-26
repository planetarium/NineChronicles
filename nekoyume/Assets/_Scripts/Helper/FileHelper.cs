using System.IO;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class FileHelper
    {
        public static void WriteAllText(string fileName, string text)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, text);
        }
        
        public static void AppendAllText(string fileName, string text)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.AppendAllText(path, text);
        }
    }
}
