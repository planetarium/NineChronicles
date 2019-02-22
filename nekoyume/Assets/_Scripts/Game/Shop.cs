using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.Game
{
    [Serializable]
    public class Shop
    {
        public Shop()
        {
            Items = new Dictionary<byte[], List<ItemBase>>();
        }

        public readonly Dictionary<byte[], List<ItemBase>> Items;

        public void Set(Address address, ItemBase item)
        {
            List<ItemBase> sales;
            byte[] addr = address.ToByteArray();
            if (!Items.TryGetValue(addr, out sales))
            {
                Items[addr] = new List<ItemBase>();
            }
            Items[addr].Add(item);
        }
    }
}
