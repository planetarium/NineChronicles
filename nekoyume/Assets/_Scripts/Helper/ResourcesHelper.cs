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
    }
}
