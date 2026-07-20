using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// Client-side display helper that reconstructs the per-stat option ("star") counts of an
    /// <see cref="Equipment"/>.
    /// <para>
    /// <see cref="ItemOptionInfo"/> only keeps a single aggregate
    /// <c>optionCountFromCombination</c> and assumes that every "extra" (duplicated) option belongs
    /// to the equipment's main stat. That assumption breaks when a non-main stat is rolled multiple
    /// times — e.g. a Transcendent armor whose main stat is HP but whose ATK option was rolled twice.
    /// In that case the extra option is silently dropped and the stat shows one fewer star than it
    /// should. Only the <em>distribution</em> is lost; the correct total is still known
    /// (<c>optionCountFromCombination</c>).
    /// </para>
    /// <para>
    /// This helper recovers the distribution from the crafting recipe. A sub-recipe lists which
    /// options can be rolled, and — verified across every shipped sub-recipe — at most one stat type
    /// is ever duplicatable within a single sub-recipe. So the missing options can be attributed to
    /// the single recipe-duplicatable stat present on the item. When the recipe cannot be resolved
    /// unambiguously it returns <see cref="ItemOptionInfo.StatOptions"/> unchanged, so the result is
    /// never worse than the legacy behaviour.
    /// </para>
    /// </summary>
    public static class EquipmentStatOptionHelper
    {
        /// <summary>
        /// Convenience overload that pulls the crafting sheets from <see cref="Game.TableSheets.Instance"/>.
        /// </summary>
        public static List<(StatType type, long value, int count)> GetStatOptions(
            Equipment equipment,
            ItemOptionInfo itemOptionInfo)
        {
            var tableSheets = Game.TableSheets.Instance;
            return GetStatOptions(
                equipment,
                itemOptionInfo,
                tableSheets == null ? null : tableSheets.EquipmentItemRecipeSheet,
                tableSheets == null ? null : tableSheets.EquipmentItemSubRecipeSheetV2,
                tableSheets == null ? null : tableSheets.EquipmentItemOptionSheet);
        }

        /// <summary>
        /// Returns the per-stat option counts to display as stars, correcting the case where a
        /// non-main stat was rolled multiple times. Falls back to
        /// <see cref="ItemOptionInfo.StatOptions"/> when the correction cannot be resolved.
        /// </summary>
        public static List<(StatType type, long value, int count)> GetStatOptions(
            Equipment equipment,
            ItemOptionInfo itemOptionInfo,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet optionSheet)
        {
            var statOptions = itemOptionInfo.StatOptions;
            if (equipment is null || statOptions.Count == 0)
            {
                return statOptions;
            }

            // Custom-craft items draw their sub-stats from CustomEquipmentCraftOptionSheet (one
            // option per stat type, so a non-main stat is never duplicated) and have no
            // EquipmentItemSubRecipeSheetV2 entry. Skipping them avoids reconstructing from a normal
            // recipe that happens to share the same base equipment id.
            if (equipment.ByCustomCraft)
            {
                return statOptions;
            }

            // Number of options that ItemOptionInfo currently attributes to stats.
            var accountedStatOptionCount = statOptions.Sum(option => option.count);
            // Number of stat options the item should actually show (total minus skill options).
            var expectedStatOptionCount =
                itemOptionInfo.OptionCountFromCombination - itemOptionInfo.SkillOptions.Count;
            var missingCount = expectedStatOptionCount - accountedStatOptionCount;
            if (missingCount <= 0)
            {
                // ItemOptionInfo already accounts for every stat option (e.g. the duplicated stat is
                // the main stat, which is handled correctly). Nothing to correct.
                return statOptions;
            }

            if (recipeSheet is null || subRecipeSheet is null || optionSheet is null)
            {
                return statOptions;
            }

            if (!TryGetDuplicatableStatType(
                    equipment.Id,
                    statOptions,
                    missingCount,
                    recipeSheet,
                    subRecipeSheet,
                    optionSheet,
                    out var duplicatableStatType))
            {
                return statOptions;
            }

            return statOptions
                .Select(option => option.type == duplicatableStatType
                    ? (option.type, option.value, option.count + missingCount)
                    : option)
                .ToList();
        }

        /// <summary>
        /// Finds the single stat type that the item's recipe allows to be duplicated and that can
        /// absorb <paramref name="missingCount"/> extra options. Returns <c>false</c> (fallback) when
        /// the recipe is unknown or the result is ambiguous.
        /// </summary>
        private static bool TryGetDuplicatableStatType(
            int equipmentId,
            IReadOnlyList<(StatType type, long value, int count)> statOptions,
            int missingCount,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet optionSheet,
            out StatType duplicatableStatType)
        {
            duplicatableStatType = StatType.NONE;

            var itemStatTypes = new HashSet<StatType>(statOptions.Select(option => option.type));

            // Reverse lookup: equipment id -> recipe(s) -> candidate sub-recipe ids.
            var subRecipeIds = recipeSheet.OrderedList
                .Where(recipe => recipe.ResultEquipmentId == equipmentId)
                .SelectMany(recipe => recipe.SubRecipeIds ?? new List<int>())
                .Distinct();

            var candidates = new HashSet<StatType>();
            foreach (var subRecipeId in subRecipeIds)
            {
                if (!subRecipeSheet.TryGetValue(subRecipeId, out var subRecipe))
                {
                    continue;
                }

                // Resolve this sub-recipe's stat options into per-stat slot counts.
                var slotCountByStatType = new Dictionary<StatType, int>();
                foreach (var optionInfo in subRecipe.Options)
                {
                    if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow) ||
                        optionRow.StatType == StatType.NONE)
                    {
                        continue;
                    }

                    slotCountByStatType.TryGetValue(optionRow.StatType, out var slotCount);
                    slotCountByStatType[optionRow.StatType] = slotCount + 1;
                }

                // The item could only have been crafted from this sub-recipe if every stat option it
                // carries is offered by the sub-recipe.
                if (!itemStatTypes.All(slotCountByStatType.ContainsKey))
                {
                    continue;
                }

                // A stat is duplicatable when the sub-recipe offers it in enough option slots to
                // cover the missing count (one slot for the base option plus missingCount extras).
                foreach (var pair in slotCountByStatType)
                {
                    if (itemStatTypes.Contains(pair.Key) &&
                        pair.Value - 1 >= missingCount)
                    {
                        candidates.Add(pair.Key);
                    }
                }
            }

            if (candidates.Count != 1)
            {
                return false;
            }

            duplicatableStatType = candidates.First();
            return true;
        }
    }
}
