using System.IO;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public class LibPlanetEditor
    {
        [MenuItem("Tools/LibPlanet/Delete All")]
        public static void CreateSpinePrefab()
        {
            var info = new DirectoryInfo(Application.persistentDataPath);
            info.Delete(true);
        }
    }   
}
