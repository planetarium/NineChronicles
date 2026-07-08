using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="EquipmentStatOptionHelper"/>, which reconstructs the per-stat option
    /// ("star") counts that <see cref="ItemOptionInfo"/> mis-attributes when a non-main stat is
    /// rolled multiple times (the "Transcendent armor with 2 ATK options shows 1 star" bug).
    /// </summary>
    public class EquipmentStatOptionHelperTest
    {
        private const int NonMainStatEquipmentId = 10200000;
        private const int NonMainStatWithSkillEquipmentId = 10200001;
        private const int MainStatEquipmentId = 10200002;
        private const int UnknownRecipeEquipmentId = 10209999;

        // 9 columns: id,stat_type,stat_min,stat_max,skill_id,skill_damage_min,skill_damage_max,skill_chance_min,skill_chance_max
        private const string OptionCsv =
            "id,stat_type,stat_min,stat_max,skill_id,skill_damage_min,skill_damage_max,skill_chance_min,skill_chance_max\n" +
            "1,ATK,1000,5000000,0,0,0,0,0\n" +
            "2,SPD,1000,5000000,0,0,0,0,0\n" +
            "3,ATK,1000,5000000,0,0,0,0,0\n" +
            "4,,0,0,1,100,200,50,50\n" +
            "5,HP,1000,50000000,0,0,0,0,0\n" +
            "6,DEF,1000,5000000,0,0,0,0,0\n" +
            "7,HP,1000,50000000,0,0,0,0,0\n";

        // 22 columns. Option ids at columns 10/13/16/19 (with ratio + required_block_index each).
        private const string SubRecipeCsv =
            "ID,required_action_point,required_gold,required_block_index,material_id,material_count,material_2_id,material_2_count,material_3_id,material_3_count,option_id,option_ratio,option_1_required_block_index,option_2_id,option_2_ratio,option_2_required_block_index,option_3_id,option_3_ratio,option_3_required_block_index,option_4_id,option_4_ratio,option_4_required_block_index\n" +
            // ATK, SPD, ATK
            "100,0,0,0,0,0,0,0,0,0,1,100,0,2,100,0,3,100,0,,,\n" +
            // ATK, SPD, ATK, Skill
            "101,0,0,0,0,0,0,0,0,0,1,100,0,2,100,0,3,100,0,4,100,0\n" +
            // HP, DEF, HP
            "200,0,0,0,0,0,0,0,0,0,5,100,0,6,100,0,7,100,0,,,\n";

        // 11 columns: id,result_equipment_id,...,sub_recipe_id,sub_recipe_id_2,sub_recipe_id_3
        private const string RecipeCsv =
            "id,result_equipment_id,material_id,material_count,required_action_point,required_gold,required_block_index,unlock_stage,sub_recipe_id,sub_recipe_id_2,sub_recipe_id_3\n" +
            "1," + NonMainStatEquipmentId + ",0,0,0,0,0,0,100,,\n" +
            "2," + NonMainStatWithSkillEquipmentId + ",0,0,0,0,0,0,101,,\n" +
            "3," + MainStatEquipmentId + ",0,0,0,0,0,0,200,,\n";

        private EquipmentItemOptionSheet _optionSheet;
        private EquipmentItemSubRecipeSheetV2 _subRecipeSheet;
        private EquipmentItemRecipeSheet _recipeSheet;
        private SkillSheet.Row _skillRow;

        [SetUp]
        public void SetUp()
        {
            _optionSheet = new EquipmentItemOptionSheet();
            _optionSheet.Set(OptionCsv);
            _subRecipeSheet = new EquipmentItemSubRecipeSheetV2();
            _subRecipeSheet.Set(SubRecipeCsv);
            _recipeSheet = new EquipmentItemRecipeSheet();
            _recipeSheet.Set(RecipeCsv);

            var skillSheet = new SkillSheet();
            skillSheet.Set("id,elemental_type,skill_type,skill_category,skill_target_type,hit_count,cool_down\n" +
                "1,Normal,Attack,NormalAttack,Enemy,1,0\n");
            _skillRow = skillSheet.First;
        }

        private static Equipment CreateArmor(int equipmentId)
        {
            var row = new EquipmentItemSheet.Row();
            // Fields are post "_name"-strip: id,item_sub_type,grade,elemental_type,set_id,stat_type,stat_value,attack_range,spine_resource_path
            row.Set(new List<string>
            {
                equipmentId.ToString(), "Armor", "8", "Normal", "0", "HP", "1000", "2", equipmentId.ToString(),
            });
            return (Equipment)ItemFactory.CreateItemUsable(row, default, 0, 0);
        }

        private List<(StatType type, long value, int count)> GetStatOptions(Equipment equipment)
        {
            var optionInfo = new ItemOptionInfo(equipment);
            return EquipmentStatOptionHelper.GetStatOptions(
                equipment, optionInfo, _recipeSheet, _subRecipeSheet, _optionSheet);
        }

        private static int CountOf(IEnumerable<(StatType type, long value, int count)> statOptions, StatType type)
        {
            return statOptions.Where(option => option.type == type).Select(option => option.count).Single();
        }

        [Test]
        public void NonMainStatDuplicated_CreditsMissingStarToThatStat()
        {
            // Transcendent armor (main stat HP) with ATK rolled twice + SPD once, no skill.
            var equipment = CreateArmor(NonMainStatEquipmentId);
            equipment.StatsMap.AddStatAdditionalValue(StatType.ATK, 6234141);
            equipment.StatsMap.AddStatAdditionalValue(StatType.SPD, 6911062);
            equipment.optionCountFromCombination = 3;

            // Sanity: the raw ItemOptionInfo under-counts ATK (the reported bug).
            var buggy = new ItemOptionInfo(equipment);
            Assert.AreEqual(1, CountOf(buggy.StatOptions, StatType.ATK));

            var statOptions = GetStatOptions(equipment);

            Assert.AreEqual(2, CountOf(statOptions, StatType.ATK));
            Assert.AreEqual(1, CountOf(statOptions, StatType.SPD));
            Assert.AreEqual(3, statOptions.Sum(option => option.count));
        }

        [Test]
        public void NonMainStatDuplicatedWithSkill_AccountsForSkillOption()
        {
            // ATK rolled twice + SPD once + a skill => optionCountFromCombination == 4.
            var equipment = CreateArmor(NonMainStatWithSkillEquipmentId);
            equipment.StatsMap.AddStatAdditionalValue(StatType.ATK, 6234141);
            equipment.StatsMap.AddStatAdditionalValue(StatType.SPD, 6911062);
            equipment.Skills.Add(SkillFactory.Get(_skillRow, 100, 5000, 0, StatType.NONE));
            equipment.optionCountFromCombination = 4;

            var statOptions = GetStatOptions(equipment);

            Assert.AreEqual(2, CountOf(statOptions, StatType.ATK));
            Assert.AreEqual(1, CountOf(statOptions, StatType.SPD));
            Assert.AreEqual(3, statOptions.Sum(option => option.count));
        }

        [Test]
        public void MainStatDuplicated_LeavesAlreadyCorrectCountsUnchanged()
        {
            // Water-like armor: main stat HP rolled twice + DEF once. ItemOptionInfo already handles
            // this correctly, so the helper must not change it (no regression).
            var equipment = CreateArmor(MainStatEquipmentId);
            equipment.StatsMap.AddStatAdditionalValue(StatType.HP, 30703968);
            equipment.StatsMap.AddStatAdditionalValue(StatType.DEF, 970021);
            equipment.optionCountFromCombination = 3;

            var statOptions = GetStatOptions(equipment);

            Assert.AreEqual(2, CountOf(statOptions, StatType.HP));
            Assert.AreEqual(1, CountOf(statOptions, StatType.DEF));
            Assert.AreEqual(3, statOptions.Sum(option => option.count));
        }

        [Test]
        public void UnknownRecipe_FallsBackToLegacyCounts()
        {
            // No recipe maps to this equipment id, so the helper cannot reconstruct and must return
            // the legacy (under-counted) values unchanged rather than guessing.
            var equipment = CreateArmor(UnknownRecipeEquipmentId);
            equipment.StatsMap.AddStatAdditionalValue(StatType.ATK, 6234141);
            equipment.StatsMap.AddStatAdditionalValue(StatType.SPD, 6911062);
            equipment.optionCountFromCombination = 3;

            var statOptions = GetStatOptions(equipment);

            Assert.AreEqual(1, CountOf(statOptions, StatType.ATK));
            Assert.AreEqual(1, CountOf(statOptions, StatType.SPD));
        }

        [Test]
        public void ByCustomCraft_FallsBackWithoutRecipeReconstruction()
        {
            // Even when a normal recipe shares the same base equipment id (ATK-duplicatable sub-recipe),
            // custom-craft items must skip reconstruction to avoid mis-attributing stars.
            var equipment = CreateArmor(NonMainStatEquipmentId);
            equipment.ByCustomCraft = true;
            equipment.StatsMap.AddStatAdditionalValue(StatType.ATK, 6234141);
            equipment.StatsMap.AddStatAdditionalValue(StatType.SPD, 6911062);
            equipment.optionCountFromCombination = 3;

            var statOptions = GetStatOptions(equipment);

            Assert.AreEqual(1, CountOf(statOptions, StatType.ATK));
            Assert.AreEqual(1, CountOf(statOptions, StatType.SPD));
        }
    }
}
