using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CombinationEquipmentQuestSheet : Sheet<int, CombinationEquipmentQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public int RecipeId { get; private set; }
            public int? SubRecipeId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                RecipeId = ParseInt(fields[3]);
                if (!string.IsNullOrEmpty(fields[4]))
                {
                    SubRecipeId = ParseInt(fields[4]);
                }
            }
        }

        public CombinationEquipmentQuestSheet() : base(nameof(CombinationEquipmentQuestSheet))
        {
        }
    }
}
