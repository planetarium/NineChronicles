using System.IO;
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
    }
}
