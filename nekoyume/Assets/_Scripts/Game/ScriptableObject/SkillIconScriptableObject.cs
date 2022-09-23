using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_SkillIcon", menuName = "Scriptable Object/Skill Icon",
        order = int.MaxValue)]
    public class SkillIconScriptableObject : ScriptableObject
    {
        public List<SkillIconData> Icons;
        public Sprite DefaultIcon;
        public GameObject SkillIconPrefab;

        [Serializable]
        public class SkillIconData
        {
            public int id;
            public Sprite icon;
        }
    }
}
