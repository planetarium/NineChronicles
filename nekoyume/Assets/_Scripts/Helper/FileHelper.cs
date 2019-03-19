using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Nekoyume.Helper
{
    public class FileHelper
    {
//        public static void Save(string fileName, string data)
//        {
//            var bf = new BinaryFormatter();
//            var fs = File.Open(Path.Combine(Application.persistentDataPath, fileName), FileMode.Open);
//            bf.Serialize(fs, data);
//            fs.Close();
//        }
//
//        public static string Load(string fileName)
//        {
//            if (!File.Exists(Path.Combine(Application.persistentDataPath, fileName)))
//            {
//                return "";
//            }
//
//            var bf = new BinaryFormatter();
//            var fs = File.Open(Path.Combine(Application.persistentDataPath, fileName), FileMode.Open);
//            return (string) bf.Deserialize(fs);
//        }

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
