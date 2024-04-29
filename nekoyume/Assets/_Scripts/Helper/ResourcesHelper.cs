using Nekoyume.UI;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class ResourcesHelper
    {
        private static readonly List<ScriptableObject> Resources = new List<ScriptableObject>();

        public static void Initialize()
        {
            var resources = UnityEngine.Resources.LoadAll<ScriptableObject>("ScriptableObject/");
            Resources.AddRange(resources);
        }

        private static T Get<T>()
        {
            var result = Resources.FirstOrDefault(x => x is T);
            if (result != null && result is T t)
            {
                return t;
            }

            NcDebug.LogError($"{typeof(T)} is not exist !");
            return default;
        }

        public static GameObject GetCharacterTitle(int grade, string text)
        {
            var datas = Get<CharacterTitleScriptableObject>().title;
            var data = datas.FirstOrDefault(x => x.Grade == grade);
            if (data == null)
            {
                return null;
            }

            data.Title.GetComponentInChildren<TextMeshProUGUI>().text = text;
            return data.Title;
        }

        public static GameObject GetAuraPrefab(int id, int level)
        {
            if (level < 0)
                return null;

            var datas = Get<AuraScriptableObject>().data;

            var auras = datas.FirstOrDefault(x => x.id == id);
            if(auras != null)
            {
                var index = AuraLevelToIndex(level);
                return auras.prefab[Mathf.Clamp(index, 0, auras.prefab.Count - 1)];
            }

            return null;
        }

        private static int AuraLevelToIndex(int level)
        {
            if (level < 2)
                return 0;

            if (level < 5)
                return 1;

            return 2;
        }

        public static GameObject GetAuraWeaponPrefab(int id, int level)
        {
            var datas = Get<WeaponAuraScriptableObject>().data;

            var auras = datas.FirstOrDefault(x => x.id == id);
            if (auras == null)
                return null;

            if(auras.prefab.Count == 1)
            {
                return auras.prefab[0];
            }

            var index = WeaponLevelToIndex(level);
            return index > -1 ? auras.prefab[index] : null;
        }

        private static int WeaponLevelToIndex(int level)
        {
            if (4 <= level && level < 7)
            {
                return 0;
            }

            if (7 <= level && level < 10)
            {
                return 1;
            }

            if (level >= 10)
            {
                return 2;
            }

            return -1;
        }

        public static List<int> GetPortalRewardLevelTable()
        {
            return Get<PortalRewardScriptalbeObject>().levelData;
        }
    }
}
