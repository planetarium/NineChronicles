using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game.Item
{
    public class Inventory : MonoBehaviour
    {
        public List<ItemBase> _items;

        public Inventory()
        {

        }

        public bool Add(ItemBase item)
        {
            return true;
        }

        public void Remove(ItemBase item)
        {

        }

        public void RemoveAt(int index)
        {

        }

        public ItemBase GetItem(int index)
        {
            return null;
        }
    }
}
