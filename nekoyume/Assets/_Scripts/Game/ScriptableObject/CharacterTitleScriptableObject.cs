using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_CharacterTitle", menuName = "Scriptable Object/Character Title",
        order = int.MaxValue)]
    public class CharacterTitleScriptableObject : ScriptableObject
    {
        public List<CharacterTitleData> title;
        public List<CharacterTitleIdOverride> titleByItemId;
    }

    [Serializable]
    public class CharacterTitleData
    {
        [SerializeField] private int grade;
        [SerializeField] private GameObject title;

        public int Grade => grade;
        public GameObject Title => title;
    }

    [Serializable]
    public class CharacterTitleIdOverride
    {
        [SerializeField] private int itemId;
        [SerializeField] private GameObject title;

        public int ItemId => itemId;
        public GameObject Title => title;
    }
}
