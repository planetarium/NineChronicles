using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public class ItemConfigForGradeSheet : Sheet<int, ItemConfigForGradeSheet.Row>
    {
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
                Id = int.Parse(fields[0]);
                MonsterPartsCountForCombination = int.Parse(fields[1]);
                MonsterPartsCountForCombinationWithNCG = int.Parse(fields[2]);
                RandomBuffSkillMinCountForCombination = int.Parse(fields[3]);
                RandomBuffSkillMaxCountForCombination = int.Parse(fields[4]);
                EnhancementLimit = int.Parse(fields[5]);
            }
        }

        public ItemConfigForGradeSheet() : base(nameof(ItemConfigForGradeSheet))
        {
        }
    }
}
