using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Helm : Equipment
    {
        public Helm(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
