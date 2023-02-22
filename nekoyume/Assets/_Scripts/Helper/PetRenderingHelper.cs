using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class PetRenderingHelper
    {
        public const string NotOwnText = "NotPossesedText";
        public const string NotOwnSlot = "NotPossesedSlot";
        public const string SummonableText = "SummonableText";
        public const string LevelUpText = "LevelUpText";
        public const string MaxLevelText = "MaxLevelText";

        private const string PetCardSpriteScriptableObjectPath = "ScriptableObject/PetRenderingData";
        private static readonly Dictionary<int, PetRenderingScriptableObject.PetRenderingData> PetRenderingData;
        private static readonly Dictionary<string, Color> PetUIPalette;

        static PetRenderingHelper()
        {
            var scriptableObject =
                Resources.Load<PetRenderingScriptableObject>(PetCardSpriteScriptableObjectPath);
            PetRenderingData = scriptableObject.PetRenderingDataList.ToDictionary(
                data => data.id,
                data => data);
            PetUIPalette = scriptableObject.PetUIPaletteList.ToDictionary(
                data => data.key,
                data => data.color);
        }

        public static Sprite GetPetCardSprite(int id)
        {
            return PetRenderingData[id].cardSlotSprite;
        }

        public static Sprite GetSoulStoneSprite(int id)
        {
            return PetRenderingData[id].soulStoneSprite;
        }

        public static SkeletonDataAsset GetPetSkeletonData(int id)
        {
            return PetRenderingData[id].spineDataAsset;
        }

        public static Color GetUIColor(string key)
        {
            return PetUIPalette[key];
        }
    }
}
