using System.IO;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public class LibplanetEditor
    {
        [MenuItem("Tools/Libplanet/Delete All")]
        public static void CreateSpinePrefab()
        {
            var info = new DirectoryInfo(Application.persistentDataPath);
            info.Delete(true);
        }
    }   
}
