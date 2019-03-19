using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class FileHelper
    {
        public static void WriteAllText(string fileName, string text)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), text);
        }
        
        public static void AppendAllText(string fileName, string text)
        {
            var path = Path.Combine(Application.persistentDataPath, fileName);
            File.AppendAllText(Path.Combine(Application.persistentDataPath, fileName), text);
        }
    }
}
