using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Data.Table;
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
            public Elemental.ElementalType ElementalType { get; private set; }
            public int SkillEffectId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                ElementalType = Enum.TryParse<Elemental.ElementalType>(fields[1], out var elementalType)
                    ? elementalType
                    : Elemental.ElementalType.Normal;
                SkillEffectId = int.TryParse(fields[2], out var skillEffectId) ? skillEffectId : 0;
            }
        }
    }

    public static class SkillSheetRowExtension
    {
        private const string DefaultIconPath = "UI/Icons/Skill/100000";

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
    }
}
