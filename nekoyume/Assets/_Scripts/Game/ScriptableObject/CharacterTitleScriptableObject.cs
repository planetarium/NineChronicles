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
    }

    [Serializable]
    public class CharacterTitleData
    {
        [SerializeField] private int grade;
        [SerializeField] private GameObject title;

        public int Grade => grade;
        public GameObject Title => title;
    }
}
