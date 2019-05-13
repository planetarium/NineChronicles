using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.Game
{
    [Serializable]
    public class Shop
    {
        public readonly Dictionary<byte[], List<ItemBase>> items;
        
        public Shop()
        {
            items = new Dictionary<byte[], List<ItemBase>>();
        }

        public void Set(Address address, ItemBase item)
        {
            var addr = address.ToByteArray();
            if (!items.ContainsKey(addr))
            {
                items.Add(addr, new List<ItemBase>());
            }
            items[addr].Add(item);
        }
    }
}
