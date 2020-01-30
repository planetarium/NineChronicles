using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Helper;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SkillSheet : Sheet<int, SkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>, IState
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public ElementalType ElementalType { get; private set; }
            public SkillType SkillType { get; private set; }
            public SkillCategory SkillCategory { get; private set; }
            public SkillTargetType SkillTargetType { get; private set; }
            public int HitCount { get; private set; }

            public Row() {}

            public Row(Bencodex.Types.Dictionary serialized)
            {
                Id = (Bencodex.Types.Integer) serialized["id"];
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType),
                    (Bencodex.Types.Text) serialized["elemental_type"]);
                SkillType = (SkillType) Enum.Parse(typeof(SkillType), (Bencodex.Types.Text) serialized["skill_type"]);
                SkillCategory = (SkillCategory) Enum.Parse(typeof(SkillCategory), (Bencodex.Types.Text) serialized["skill_category"]);
                SkillTargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), (Bencodex.Types.Text) serialized["skill_target_type"]);
                HitCount = (Bencodex.Types.Integer) serialized["hit_count"];
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), fields[1]);
                SkillType = (SkillType) Enum.Parse(typeof(SkillType), fields[2]);
                SkillCategory = (SkillCategory) Enum.Parse(typeof(SkillCategory), fields[3]);
                SkillTargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), fields[4]);
                HitCount = int.Parse(fields[5]);
            }
            public IValue Serialize() =>
                Bencodex.Types.Dictionary.Empty
                    .Add("id", Id)
                    .Add("elemental_type", ElementalType.ToString())
                    .Add("skill_type", SkillType.ToString())
                    .Add("skill_category", SkillCategory.ToString())
                    .Add("skill_target_type", SkillTargetType.ToString())
                    .Add("hit_count", HitCount);

            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }
        }

        public SkillSheet() : base(nameof(SkillSheet))
        {
        }
    }

    public static class SkillSheetExtension
    {
        private static readonly Dictionary<int, List<BuffSheet.Row>> SkillBuffs =
            new Dictionary<int, List<BuffSheet.Row>>();

        public static string GetLocalizedName(this SkillSheet.Row row)
        {
            return LocalizationManager.Localize($"SKILL_NAME_{row.Id}");
        }

        public static Sprite GetIcon(this SkillSheet.Row row)
        {
            return SpriteHelper.GetSkillIcon(row.Id);
        }

        // 매번 결정적인 결과를 리턴하기에 캐싱함.
        public static List<BuffSheet.Row> GetBuffs(this SkillSheet.Row row)
        {
            if (SkillBuffs.ContainsKey(row.Id))
                return SkillBuffs[row.Id];

            var buffs = new List<BuffSheet.Row>();
            SkillBuffs[row.Id] = buffs;

            var skillBuffSheet = Game.Game.instance.TableSheets.SkillBuffSheet;
            if (!skillBuffSheet.TryGetValue(row.Id, out var skillBuffRow))
                return buffs;

            var buffSheet = Game.Game.instance.TableSheets.BuffSheet;
            foreach (var buffId in skillBuffRow.BuffIds)
            {
                if (!buffSheet.TryGetValue(buffId, out var buffRow))
                    continue;

                buffs.Add(buffRow);
            }

            return buffs;
        }
    }
}
