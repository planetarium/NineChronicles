using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public class EquipmentItemSubRecipeSheet : Sheet<int, EquipmentItemSubRecipeSheet.Row>
    {
        public struct MaterialInfo
        {
            public readonly int Id;
            public readonly int Count;

            public MaterialInfo(int id, int count)
            {
                Id = id;
                Count = count;
            }
        }

        public struct OptionInfo
        {
            public readonly int Id;
            public readonly int Ratio;

            public OptionInfo(int id, int ratio)
            {
                Id = id;
                Ratio = ratio;
            }
        }
        public class Row: SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int RequiredActionPoint { get; private set; }
            public long RequiredGold { get; private set; }
            public int UnlockStage { get; private set; }
            public List<MaterialInfo> Materials { get; private set; }
            public List<OptionInfo> Options { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                RequiredActionPoint = int.Parse(fields[1]);
                RequiredGold = long.Parse(fields[2]);
                UnlockStage = int.Parse(fields[3]);
                Materials = new List<MaterialInfo>();
                Options = new List<OptionInfo>();
                for (var i = 0; i < 3; i++)
                {
                    var offSet = i * 2;
                    Materials.Add(new MaterialInfo(int.Parse(fields[4 + offSet]), int.Parse(fields[5 + offSet])));
                }
                for (var i = 0; i < 4; i++)
                {
                    var offSet = i * 2;
                    if (string.IsNullOrEmpty(fields[10 + offSet]))
                        continue;
                    Options.Add(new OptionInfo(int.Parse(fields[10 + offSet]), int.Parse(fields[11 + offSet])));
                }
            }
        }

        public EquipmentItemSubRecipeSheet() : base(nameof(EquipmentItemSubRecipeSheet))
        {
        }
    }
}
