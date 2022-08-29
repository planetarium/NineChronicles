using System;
using System.Collections.Generic;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ActionBuffSheet : Sheet<int, ActionBuffSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public int GroupId { get; private set; }
            public int Chance { get; private set; }
            public int Duration { get; private set; }
            public SkillTargetType TargetType { get; private set; }
            public ActionBuffType ActionBuffType { get; private set; }
            public ElementalType ElementalType { get; private set; }
            public decimal ATKPowerRatio { get; private set; }


            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                GroupId = ParseInt(fields[1]);
                Chance = ParseInt(fields[2]);
                Duration = ParseInt(fields[3]);
                TargetType = (SkillTargetType)Enum.Parse(typeof(SkillTargetType), fields[4]);
                ActionBuffType = (ActionBuffType)Enum.Parse(typeof(ActionBuffType), fields[5]);
                ElementalType = (ElementalType)Enum.Parse(typeof(ElementalType), fields[6]);
                ATKPowerRatio = ParseDecimal(fields[7]);
            }
        }

        public ActionBuffSheet() : base(nameof(ActionBuffSheet))
        {
        }
    }
}
