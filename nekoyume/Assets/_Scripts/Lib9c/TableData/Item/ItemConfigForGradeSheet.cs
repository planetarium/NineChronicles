using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ItemConfigForGradeSheet : Sheet<int, ItemConfigForGradeSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            
            public int Id { get; private set; }
            public int MonsterPartsCountForCombination { get; private set; }
            public int MonsterPartsCountForCombinationWithNCG { get; private set; }
            public int RandomBuffSkillMinCountForCombination { get; private set; }
            public int RandomBuffSkillMaxCountForCombination { get; private set; }
            public int EnhancementLimit { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                MonsterPartsCountForCombination = ParseInt(fields[1]);
                MonsterPartsCountForCombinationWithNCG = ParseInt(fields[2]);
                RandomBuffSkillMinCountForCombination = ParseInt(fields[3]);
                RandomBuffSkillMaxCountForCombination = ParseInt(fields[4]);
                EnhancementLimit = ParseInt(fields[5]);
            }
        }

        public ItemConfigForGradeSheet() : base(nameof(ItemConfigForGradeSheet))
        {
        }
    }
}
