using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Pet;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Pet;
using Nekoyume.UI;
using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class PetFrontHelper
    {
        public const string LevelUpText = "LevelUpText";
        public const string MaxLevelText = "MaxLevelText";
        public const string SoulStoneGaugeMax = "SoulStoneGaugeMax";
        public const string SoulStoneGaugeSummon = "SoulStoneGaugeSummon";
        public const string SoulStoneGaugeLevelUp = "SoulStoneGaugeLevelUp";

        private const string PetCardSpriteScriptableObjectPath = "ScriptableObject/PetRenderingData";
        private static readonly Dictionary<int, PetRenderingScriptableObject.PetRenderingData> PetRenderingData;
        private static readonly Dictionary<string, Color> PetUIPalette;
        private const string BlockIndexFormat = "<style=G5> {0}-{1} <style=SymbolAfter> <color=#{2}><style=G5> {3}-{4} (-{5})</color>";
        private const string HourglassFormat = "<style=G2> {0} <style=SymbolAfter> <color=#{1}><style=G2> {2} (+{3})</color>";
        private const string CrystalFormat = "<style=G1> {0} <style=SymbolAfter> <color=#{1}><style=G1> {2} (-{3}%)</color>";
        private const string StatOptionFormat = "<sprite name=\"icon_Stats\"> {0}%  <style=SymbolAfter> <sprite name=\"icon_Stats\"> <color=#{1}>{2}%</color>";
        private const string SkillOptionFormat = "<style=Skill> {0}%  <style=SymbolAfter> <style=Skill> <color=#{1}>{2}%</color>";

        static PetFrontHelper()
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

        public static Vector3 GetLocalPositionInCard(int id)
        {
            return PetRenderingData[id].localPosition;
        }

        public static Vector3 GetLocalScaleInCard(int id)
        {
            return PetRenderingData[id].localScale;
        }

        public static Color GetUIColor(string key)
        {
            return PetUIPalette[key];
        }

        public static bool HasNotification(int id)
        {
            var nextLevel = 1;
            if (States.Instance.PetStates.TryGetPetState(id, out var pet))
            {
                nextLevel = pet.Level + 1;
            }

            var isMaxLevel = !TableSheets.Instance.PetCostSheet[id]
                .TryGetCost(nextLevel, out var nextCost);
            if (isMaxLevel)
            {
                return false;
            }

            var ncgCost = States.Instance.GoldBalanceState.Gold.Currency * nextCost.NcgQuantity;
            var soulStoneCost =
                PetHelper.GetSoulstoneCurrency(TableSheets.Instance.PetSheet[id].SoulStoneTicker) *
                nextCost.SoulStoneQuantity;
            return States.Instance.GoldBalanceState.Gold >= ncgCost &&
                States.Instance.CurrentAvatarBalances.TryGetValue(
                    soulStoneCost.Currency.Ticker,
                    out var soulStone) &&
                soulStone >= soulStoneCost;
        }

        public static (string description, bool isApplied) GetDescriptionText(
            PetOptionSheet.Row.PetOptionInfo optionInfo,
            Craft.CraftInfo craftInfo,
            PetState petState,
            GameConfigState gameConfigState)
        {
            var appliedColorHex = Palette.GetColor(EnumType.ColorType.TextPositive).ColorToHex();

            switch (optionInfo.OptionType)
            {
                case PetOptionType.ReduceRequiredBlock:
                case PetOptionType.ReduceRequiredBlockByFixedValue:
                    var isFixedValue =
                        optionInfo.OptionType == PetOptionType.ReduceRequiredBlockByFixedValue;
                    var requiredMin = PetHelper.CalculateReducedBlockOnCraft(
                        craftInfo.RequiredBlockMin,
                        0,
                        petState,
                        TableSheets.Instance.PetOptionSheet);
                    var requiredMax = PetHelper.CalculateReducedBlockOnCraft(
                        craftInfo.RequiredBlockMax,
                        0,
                        petState,
                        TableSheets.Instance.PetOptionSheet);
                    if (requiredMax == craftInfo.RequiredBlockMax)
                    {
                        var defaultDescription =
                            GetDefaultDescriptionText(optionInfo, gameConfigState);
                        return (defaultDescription, false);
                    }

                    return (string.Format(
                        BlockIndexFormat,
                        craftInfo.RequiredBlockMin.ToCurrencyNotation(),
                        craftInfo.RequiredBlockMax.ToCurrencyNotation(),
                        appliedColorHex,
                        requiredMin.ToCurrencyNotation(),
                        requiredMax.ToCurrencyNotation(),
                        isFixedValue ? optionInfo.OptionValue : $"{optionInfo.OptionValue}%"), true);
                case PetOptionType.AdditionalOptionRate:
                case PetOptionType.AdditionalOptionRateByFixedValue:
                    if (!TableSheets.Instance.EquipmentItemSubRecipeSheetV2
                        .TryGetValue(craftInfo.SubrecipeId, out var subrecipeRow))
                    {
                        var defaultDescription =
                            GetDefaultDescriptionText(optionInfo, gameConfigState);
                        return (defaultDescription, false);
                    }

                    // 3,4 옵션이 존재하지 않는 아이템의 경우 100%로만 표기
                    var statView = string.Format(StatOptionFormat, 100, appliedColorHex, 100);
                    var skillView = string.Empty;
                    foreach (var equipmentOptionInfo in subrecipeRow.Options)
                    {
                        var ratio = PetHelper.GetBonusOptionProbability(
                            equipmentOptionInfo.Ratio,
                            petState,
                            TableSheets.Instance.PetOptionSheet) / 100m;
                        var fixedRatio = Math.Min(ratio, 100m);
                        if (TableSheets.Instance.EquipmentItemOptionSheet
                            .TryGetValue(equipmentOptionInfo.Id, out var optionRow))
                        {
                            if (optionRow.Id % 10 <= 2)
                            {
                                continue;
                            }

                            if (optionRow.SkillId == default)
                            {
                                statView = string.Format(
                                    StatOptionFormat,
                                    equipmentOptionInfo.Ratio / 100m,
                                    appliedColorHex,
                                    fixedRatio);
                            }
                            else
                            {
                                skillView = string.Format(
                                    SkillOptionFormat,
                                    equipmentOptionInfo.Ratio / 100m,
                                    appliedColorHex,
                                    fixedRatio);
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(skillView))
                    {
                        return (statView, true);
                    }
                    
                    return ($"{skillView}  {statView}", true);
                case PetOptionType.IncreaseBlockPerHourglass:
                    return (string.Format(
                        HourglassFormat,
                        gameConfigState.HourglassPerBlock.ToCurrencyNotation(),
                        appliedColorHex,
                        (gameConfigState.HourglassPerBlock + optionInfo.OptionValue).ToCurrencyNotation(),
                        optionInfo.OptionValue.ToCurrencyNotation()), true);
                case PetOptionType.DiscountMaterialCostCrystal:
                    var cost = PetHelper.CalculateDiscountedMaterialCost(
                        craftInfo.CostCrystal,
                        petState,
                        TableSheets.Instance.PetOptionSheet);
                    if (craftInfo.CostCrystal.MajorUnit == cost.MajorUnit)
                    {
                        var defaultDescription =
                            GetDefaultDescriptionText(optionInfo, gameConfigState);
                        return (defaultDescription, false);
                    }

                    return (string.Format(
                        CrystalFormat,
                        craftInfo.CostCrystal.MajorUnit.ToCurrencyNotation(),
                        appliedColorHex,
                        cost.MajorUnit.ToCurrencyNotation(),
                        optionInfo.OptionValue), true);
                default:
                    var desc =
                        GetDefaultDescriptionText(optionInfo, gameConfigState);
                    return (desc, false);
            }
        }

        public static string GetDefaultDescriptionText(
            PetOptionSheet.Row.PetOptionInfo optionInfo,
            GameConfigState gameConfigState)
        {
            if (optionInfo.OptionType == PetOptionType.IncreaseBlockPerHourglass)
            {
                var originalValue = gameConfigState.HourglassPerBlock;
                var optionValue = optionInfo.OptionValue;
                var optionValueText = $"{originalValue + optionValue} ({originalValue}+{optionValue})";
                return L10nManager.Localize(
                    $"PET_DESCRIPTION_{optionInfo.OptionType}",
                    optionValueText);
            }
            else
            {
                return L10nManager.Localize(
                    $"PET_DESCRIPTION_{optionInfo.OptionType}",
                    optionInfo.OptionValue);
            }
        }

        public static string GetComparisonDescriptionText(
            PetOptionSheet.Row.PetOptionInfo currentOption,
            PetOptionSheet.Row.PetOptionInfo targetOption)
        {
            if (currentOption.OptionType == PetOptionType.IncreaseBlockPerHourglass)
            {
                var originalValue = States.Instance.GameConfigState.HourglassPerBlock;
                var optionValue = currentOption.OptionValue;
                var currentOptionValueText = $"{originalValue + optionValue} ({originalValue}+{optionValue})";
                var targetOptionValue = targetOption.OptionValue;
                var targetOptionValueText = $"{originalValue + targetOptionValue} ({originalValue}+{targetOptionValue})";
                return L10nManager.Localize($"PET_DESCRIPTION_TWO_OPTION_{targetOption.OptionType}",
                    currentOptionValueText,
                    targetOptionValueText);
            }
            else
            {
                return L10nManager.Localize($"PET_DESCRIPTION_TWO_OPTION_{targetOption.OptionType}",
                    currentOption.OptionValue,
                    targetOption.OptionValue);
            }
        }
    }
}
