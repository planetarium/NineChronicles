using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
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
            public SkillType skillType { get; private set; }
            public SkillCategory skillCategory { get; private set; }
            public SkillTargetType skillTargetType { get; private set; }
            public int hitCount { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), fields[1]);
                skillType = (SkillType) Enum.Parse(typeof(SkillType), fields[2]);
                skillCategory = (SkillCategory) Enum.Parse(typeof(SkillCategory), fields[3]);
                skillTargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), fields[4]);
                hitCount = int.Parse(fields[5]);
            }

            public IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "key"] = (Integer) Key,
                });

            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                var key = (int) ((Integer) serialized[(Text) "key"]).Value;
                return Game.Game.instance.TableSheets.SkillSheet[key];
            }
        }

        public SkillSheet() : base(nameof(SkillSheet))
        {
        }
    }

    public static class SkillSheetExtension
    {
        private const string DefaultIconPath = "UI/Icons/Skill/100000";

        private static readonly Dictionary<int, List<BuffSheet.Row>> SkillBuffs =
            new Dictionary<int, List<BuffSheet.Row>>();

        public static string GetLocalizedName(this SkillSheet.Row row)
        {
            return LocalizationManager.Localize($"SKILL_NAME_{row.Id}");
        }

        public static Sprite GetIcon(this SkillSheet.Row row)
        {
            var path = $"UI/Icons/Skill/{row.Id}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite)
            {
                return sprite;
            }

            sprite = Resources.Load<Sprite>(DefaultIconPath);

            return sprite;
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
