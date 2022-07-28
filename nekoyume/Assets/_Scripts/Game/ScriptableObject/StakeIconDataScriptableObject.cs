using Nekoyume.State.Subjects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_StakeIconData", menuName = "Scriptable Object/Stake Icon Data",
        order = int.MaxValue)]
    public class StakeIconDataScriptableObject : ScriptableObject
    {
        [Serializable]
        private struct IconData
        {
            public int Level;
            public Sprite IconSprite;
        }

        [SerializeField]
        private Sprite fallbackIconSprite;

        [SerializeField]
        private Sprite fallbackSmallIconSprite;

        [SerializeField]
        private List<IconData> iconList;

        [SerializeField]
        private List<IconData> smallIconList;

        public Sprite GetIcon(int level, bool smallIcon)
        {
            var data = smallIcon ?
                smallIconList.Find(x => x.Level == level) :
                iconList.Find(x => x.Level == level);

            if (!data.IconSprite)
            {
                return smallIcon ?
                    fallbackSmallIconSprite : fallbackIconSprite;
            }

            return data.IconSprite;
        }
    }
}
