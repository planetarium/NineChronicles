using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Consumable : ItemUsable
    {
        public new ConsumableItemSheet.Row Data { get; }

        public Consumable(ConsumableItemSheet.Row data, Guid id) : base(data, id)
        {
            Data = data;
        }
    }
}
