using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public enum IconType
    {
        Default,
        Small,
        Bubble,
    }

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
        private Sprite fallbackBubbleIconSprite;

        [SerializeField]
        private List<IconData> iconList;

        [SerializeField]
        private List<IconData> smallIconList;

        [SerializeField]
        private List<IconData> bubbleIconList;

        public Sprite GetIcon(int level, IconType iconType)
        {
            var list = iconType switch
            {
                IconType.Default => iconList,
                IconType.Small => smallIconList,
                IconType.Bubble => bubbleIconList,
                _ => new List<IconData>()
            };
            var data = list.Find(x => x.Level == level);

            if (!data.IconSprite)
            {
                return iconType switch
                {
                    IconType.Small => fallbackSmallIconSprite,
                    IconType.Bubble => fallbackBubbleIconSprite,
                    IconType.Default or _ => fallbackIconSprite,
                };
            }

            return data.IconSprite;
        }
    }
}
