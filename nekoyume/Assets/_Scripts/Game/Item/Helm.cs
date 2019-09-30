using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Helm : Equipment
    {
        public Helm(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
