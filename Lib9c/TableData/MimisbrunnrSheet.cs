using System;
using System.Collections.Generic;
using Nekoyume.Model.Elemental;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MimisbrunnrSheet : Sheet<int, MimisbrunnrSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<ElementalType> ElementalTypes { get; private set; }
        
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ElementalTypes = new List<ElementalType>();
                for (int i = 1; i < 6; ++i)
                {
                    if (!string.IsNullOrEmpty(fields[i]))
                    {
                        var type = (ElementalType) Enum.Parse(typeof(ElementalType), fields[i]);
                        ElementalTypes.Add(type);
                    }
                }
            }
        }
        
        public MimisbrunnrSheet() : base(nameof(MimisbrunnrSheet))
        {
        }
    }
}
