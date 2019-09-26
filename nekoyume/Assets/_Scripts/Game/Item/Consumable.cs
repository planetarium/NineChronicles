using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Consumable : ItemUsable
    {
        // ItemUsable.Data가 ItemConsumableSheet.Row형이기 때문에 생략함.
//        public new ItemConsumableSheet.Row Data { get; }
        
        public Consumable(ConsumableItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
