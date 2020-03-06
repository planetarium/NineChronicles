using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;

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
            public int Cooldown { get; private set; }

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
                Cooldown = (Bencodex.Types.Integer) serialized["cooldown"];
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), fields[1]);
                SkillType = (SkillType) Enum.Parse(typeof(SkillType), fields[2]);
                SkillCategory = (SkillCategory) Enum.Parse(typeof(SkillCategory), fields[3]);
                SkillTargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), fields[4]);
                HitCount = ParseInt(fields[5]);
                Cooldown = ParseInt(fields[6]);
            }
            public IValue Serialize() =>
                Bencodex.Types.Dictionary.Empty
                    .Add("id", Id)
                    .Add("elemental_type", ElementalType.ToString())
                    .Add("skill_type", SkillType.ToString())
                    .Add("skill_category", SkillCategory.ToString())
                    .Add("skill_target_type", SkillTargetType.ToString())
                    .Add("hit_count", HitCount)
                    .Add("cooldown", Cooldown);

            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }
        }

        public SkillSheet() : base(nameof(SkillSheet))
        {
        }
    }
}
