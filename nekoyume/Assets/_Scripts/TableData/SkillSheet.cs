using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SkillSheet : Sheet<int, SkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public ElementalType ElementalType { get; private set; }
            public int SkillEffectId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                ElementalType = Enum.TryParse<ElementalType>(fields[1], out var elementalType)
                    ? elementalType
                    : ElementalType.Normal;
                SkillEffectId = int.TryParse(fields[2], out var skillEffectId) ? skillEffectId : 0;
            }
        }
        
        public SkillSheet() : base(nameof(SkillSheet))
        {
        }
    }

    public static class SkillSheetExtension
    {
        private const string DefaultIconPath = "UI/Icons/Skill/100000";
        
        private static readonly Dictionary<int, List<BuffSheet.Row>> SkillBuffs = new Dictionary<int, List<BuffSheet.Row>>(); 

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
