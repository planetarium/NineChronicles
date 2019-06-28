using System.IO;
using Nekoyume.BlockChain;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public static class LibplanetEditor
    {
        [MenuItem("Tools/Libplanet/Delete All(Editor)")]
        public static void DeleteAllEditor()
        {
            var path = Path.Combine(Application.persistentDataPath, "planetarium_dev.ldb");
            DeleteAll(path);
        }
        
        [MenuItem("Tools/Libplanet/Delete All(Player)")]
        public static void DeleteAllPlayer()
        {
            var path = Path.Combine(Application.persistentDataPath, "planetarium.ldb");
            DeleteAll(path);
        }

        private static void DeleteAll(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return;
            }
            info.Delete();
            
            // Todo. PlayerPrefs에 비밀키를 저장하는 방식을 파일에 쓰는 방식으로 바꾸는 것이 좋겠음.
            // Player 모드에서 관리되는 PlayerPrefs 보다는 파일을 관리하는 방식이 쉬워서?  
            PlayerPrefs.DeleteKey(AgentController.PlayerPrefsKeyOfAgentPrivateKey);
            for (var i = 0; i < 3; i++)
            {
                PlayerPrefs.DeleteKey(string.Format(AvatarManager.PrivateKeyFormat, i));                
            }
        }
    }   
}
