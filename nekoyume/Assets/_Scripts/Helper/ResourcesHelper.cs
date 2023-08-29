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

        public static IEnumerator CoInitialize()
        {
            yield return null;
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

            Debug.LogError($"{typeof(T)} is not exist !");
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

        public static GameObject GetAuraWeaponPrefab(int id, int level)
        {
            var datas = Get<WeaponAuraScriptableObject>().data;

            var auras = datas.FirstOrDefault(x => x.id == id);
            if (auras != null)
            {
                var index = WeaponLevelToIndex(level);
                return index > -1 ? auras.prefab[index] : null;
            }

            return null;
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
    }
}
