using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.TableData
{
    [Serializable]
    public class BuffSheet : Sheet<int, BuffSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => id;
            public int id;
            public BuffCategory category;
            public SkillTargetType targetType;
            public int effect;
            public int time;
            public int skillId;
            public int chance;
            public override void Set(IReadOnlyList<string> fields)
            {
                int.TryParse(fields[0], out id);
                Enum.TryParse(fields[2], out category);
                Enum.TryParse(fields[3], out targetType);
                int.TryParse(fields[4], out effect);
                int.TryParse(fields[5], out time);
                int.TryParse(fields[7], out chance);
            }
        }
    }

    public static class BuffSheetRowExtension
    {
        private const string DefaultIconPath = "UI/Icons/Buff/1";

        public static Sprite GetIcon(this BuffSheet.Row row)
        {
            var path = $"UI/Icons/Buff/{row.id}";
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
