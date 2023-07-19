using System;
using Lib9c;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Pet;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;

namespace Nekoyume.Helper
{
    public static class PetHelper
    {
        public static Currency GetSoulstoneCurrency(string ticker) =>
            Currencies.GetSoulStone(ticker);

        public static (int ncgQuantity, int soulStoneQuantity) CalculateEnhancementCost(
            PetCostSheet costSheet,
            int petId,
            int currentLevel,
            int targetLevel)
        {
            if (costSheet is null)
            {
                throw new ArgumentNullException(nameof(costSheet));
            }

            if (petId < 0)
            {
                throw new ArgumentException(
                    $"{nameof(petId)} must be greater than or equal to 0.");
            }

            if (currentLevel < 0)
            {
                throw new ArgumentException(
                    $"{nameof(currentLevel)} must be greater than or equal to 0.");
            }

            if (targetLevel < 1)
            {
                throw new ArgumentException(
                    $"{nameof(targetLevel)} must be greater than or equal to 0.");
            }

            if (currentLevel >= targetLevel)
            {
                throw new ArgumentException(
                    $"{nameof(currentLevel)} must be less than {nameof(targetLevel)}.");
            }

            if (!costSheet.TryGetValue(petId, out var row))
            {
                throw new SheetRowNotFoundException(nameof(PetCostSheet), petId);
            }

            var costList = row.Cost;
            var range = targetLevel - currentLevel;
            var startIndex = currentLevel != 0
                ? costList.FindIndex(cost => cost.Level == currentLevel) + 1
                : 0;
            var ncgCost = 0;
            var soulStoneCost = 0;
            costList.GetRange(startIndex, range).ForEach(cost =>
            {
                ncgCost += cost.NcgQuantity;
                soulStoneCost += cost.SoulStoneQuantity;
            });
            return (ncgCost, soulStoneCost);
        }

        public static FungibleAssetValue CalculateDiscountedMaterialCost(
            FungibleAssetValue originalCost,
            PetState petState,
            PetOptionSheet petOptionSheet)
        {
            if (originalCost.MajorUnit <= 0 ||
                !petOptionSheet.TryGetValue(petState.PetId, out var optionRow) ||
                !optionRow.LevelOptionMap.TryGetValue(petState.Level, out var optionInfo))
            {
                return originalCost;
            }
            else if (optionInfo.OptionType == PetOptionType.DiscountMaterialCostCrystal)
            {
                // Convert as permyriad
                var multiplier = (int)(10000 - optionInfo.OptionValue * 100);
                var cost = originalCost.DivRem(10000, out _) * multiplier;

                // Keep cost more than 1.
                if (cost.MajorUnit <= 0)
                {
                    cost = 1 * CrystalCalculator.CRYSTAL;
                }

                return cost;
            }

            return originalCost;
        }

        public static long CalculateReducedBlockOnCraft(
            long originalBlock,
            long minimumBlock,
            PetState petState,
            PetOptionSheet petOptionSheet)
        {
            if (originalBlock <= minimumBlock ||
                !petOptionSheet.TryGetValue(petState.PetId, out var optionRow) ||
                !optionRow.LevelOptionMap.TryGetValue(petState.Level, out var optionInfo))
            {
                return originalBlock;
            }
            else if (optionInfo.OptionType == PetOptionType.ReduceRequiredBlock)
            {
                var multiplier = (100 - optionInfo.OptionValue) / 100;
                var result = (long)Math.Round(originalBlock * multiplier);
                return Math.Max(minimumBlock, result);
            }
            else if (optionInfo.OptionType == PetOptionType.ReduceRequiredBlockByFixedValue)
            {
                return Math.Max(minimumBlock, originalBlock - (long)optionInfo.OptionValue);
            }

            return originalBlock;
        }

        public static int GetBonusOptionProbability(
            int originalRatio,
            PetState petState,
            PetOptionSheet petOptionSheet)
        {
            if (!petOptionSheet.TryGetValue(petState.PetId, out var optionRow) ||
                !optionRow.LevelOptionMap.TryGetValue(petState.Level, out var optionInfo))
            {
                return originalRatio;
            }
            else if (optionInfo.OptionType == PetOptionType.AdditionalOptionRate)
            {
                var multiplier = (100 + optionInfo.OptionValue) / 100;
                var result = (int)Math.Round(originalRatio * multiplier);
                return result;
            }
            else if (optionInfo.OptionType == PetOptionType.AdditionalOptionRateByFixedValue)
            {
                return originalRatio + (int)(optionInfo.OptionValue * 100);
            }

            return originalRatio;
        }

        public static int CalculateDiscountedHourglass(
            long diff,
            int hourglassPerBlock,
            PetState petState,
            PetOptionSheet petOptionSheet)
        {
            if (!petOptionSheet.TryGetValue(petState.PetId, out var optionRow) ||
                !optionRow.LevelOptionMap.TryGetValue(petState.Level, out var optionInfo))
            {
                return RapidCombination0.CalculateHourglassCount(hourglassPerBlock, diff);
            }
            else
            {
                if (optionInfo.OptionType == PetOptionType.IncreaseBlockPerHourglass)
                {
                    var increasedHourglassPerBlock = hourglassPerBlock
                        + optionInfo.OptionValue;
                    return RapidCombination0.CalculateHourglassCount(
                        increasedHourglassPerBlock,
                        diff);
                }
            }

            return RapidCombination0.CalculateHourglassCount(hourglassPerBlock, diff);
        }
    }
}
