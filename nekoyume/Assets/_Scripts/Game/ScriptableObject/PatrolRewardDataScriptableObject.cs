using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_PatrolRewardData", menuName = "Scriptable Object/Patrol Reward Data",
        order = int.MaxValue)]
    public class PatrolRewardDataScriptableObject : ScriptableObject
    {
        [SerializeField]
        private Sprite fallbackIconSprite;

        [SerializeField]
        private List<IconData> iconList;

        [Serializable]
        private struct IconData
        {
            public int Level;
            public Sprite IconSprite;
        }

        public Sprite GetIcon(int level)
        {
            var data = iconList.Find(x => x.Level == level);

            return data.IconSprite ? data.IconSprite : fallbackIconSprite;
        }
    }
}
