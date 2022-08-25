using System;
using System.Collections.Generic;
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

            public SkillTargetType TargetType { get; private set; }

            public ActionBuffType ActionBuffType { get; private set; }

            /// <summary>
            /// Turn count.
            /// </summary>
            public int Duration { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                GroupId = ParseInt(fields[1]);
                TargetType = (SkillTargetType)Enum.Parse(typeof(SkillTargetType), fields[2]);
                ActionBuffType = (ActionBuffType)Enum.Parse(typeof(ActionBuffType), fields[3]);
                Duration = ParseInt(fields[4]);
            }
        }

        public ActionBuffSheet() : base(nameof(ActionBuffSheet))
        {
        }
    }
}
