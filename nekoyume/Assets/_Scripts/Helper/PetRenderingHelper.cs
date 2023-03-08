using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.Game;
using Nekoyume.State;
using Spine.Unity;
using Unity.Mathematics;
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

        public static Sprite GetSoulStoneSprite(int id)
        {
            return PetRenderingData[id].soulStoneSprite;
        }

        public static SkeletonDataAsset GetPetSkeletonData(int id)
        {
            return PetRenderingData[id].spineDataAsset;
        }

        public static float3 GetHsv(int id)
        {
            return PetRenderingData[id].hsv;
        }

        public static Color GetUIColor(string key)
        {
            return PetUIPalette[key];
        }

        public static bool HasNotification(int id)
        {
            var currentLevel = 0;
            if (States.Instance.PetStates.TryGetPetState(id, out var pet))
            {
                currentLevel = pet.Level;
            }

            var costList = TableSheets.Instance.PetCostSheet[id].Cost
                .OrderBy(cost => cost.Level)
                .ToList();
            if (costList.Last().Level == currentLevel)
            {
                return false;
            }

            var needCost = costList[currentLevel];
            var ncgCost = States.Instance.GoldBalanceState.Gold.Currency * needCost.NcgQuantity;
            var soulStoneCost =
                Currency.Legacy(TableSheets.Instance.PetSheet[id].SoulStoneTicker, 0, null) *
                needCost.SoulStoneQuantity;
            return States.Instance.GoldBalanceState.Gold >= ncgCost &&
                   States.Instance.AvatarBalance.TryGetValue(
                       soulStoneCost.Currency.Ticker,
                       out var soulStone) &&
                   soulStone >= soulStoneCost;
        }
    }
}
