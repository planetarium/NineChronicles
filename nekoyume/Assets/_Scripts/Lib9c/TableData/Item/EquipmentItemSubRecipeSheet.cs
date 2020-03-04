using System;
using System.Collections.Generic;
using Serilog;
using static Nekoyume.TableData.TableExtensions;

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

        public class Row : SheetRow<int>
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
                Id = ParseInt(fields[0]);
                RequiredActionPoint = ParseInt(fields[1]);
                RequiredGold = ParseLong(fields[2]);
                UnlockStage = ParseInt(fields[3]);
                Materials = new List<MaterialInfo>();
                Options = new List<OptionInfo>();
                for (var i = 0; i < 3; i++)
                {
                    var offset = i * 2;
                    try
                    {
                        Materials.Add(new MaterialInfo(ParseInt(fields[4 + offset]), ParseInt(fields[5 + offset])));
                    }
                    catch (ArgumentException)
                    {
                        Log.Debug(
                            $"[{nameof(EquipmentItemSubRecipeSheet)}]{nameof(fields)}[{4 + offset}] or {nameof(fields)}[{5 + offset}] is null");
                    }
                }

                for (var i = 0; i < 4; i++)
                {
                    var offset = i * 2;
                    if (string.IsNullOrEmpty(fields[10 + offset]) || string.IsNullOrEmpty(fields[11 + offset]))
                        continue;
                    try
                    {
                        Options.Add(new OptionInfo(ParseInt(fields[10 + offset]), ParseInt(fields[11 + offset])));
                    }
                    catch (ArgumentException)
                    {
                        Log.Debug(
                            $"[{nameof(EquipmentItemSubRecipeSheet)}]{nameof(fields)}[{10 + offset}] or {nameof(fields)}[{11 + offset}] is null");
                    }
                }
            }
        }

        public EquipmentItemSubRecipeSheet() : base(nameof(EquipmentItemSubRecipeSheet))
        {
        }
    }
}
