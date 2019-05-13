using System;
using Nekoyume.Game.Item;
using UniRx;
using Inventory = Nekoyume.Game.Item.Inventory;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume
{
    [Serializable]
    public class IntReactiveProperty : ReactiveProperty<int>
    {
        public IntReactiveProperty()
        {
        }
        
        public IntReactiveProperty(int value) : base(value)
        {
        }
    }
    
    [Serializable]
    public class ByteArrayReactiveProperty : ReactiveProperty<byte[]>
    {
    }
    
    [Serializable]
    public class DecimalReactiveProperty : ReactiveProperty<decimal>
    {
        public DecimalReactiveProperty()
        {
        }

        public DecimalReactiveProperty(decimal value) : base(value)
        {
        }
    }
    
    [Serializable]
    public class ItemBaseReactiveProperty : ReactiveProperty<ItemBase>
    {
    }
    
    [Serializable]
    public class InventoryItemReactiveProperty : ReactiveProperty<Inventory.InventoryItem>
    {
    }
    
    [Serializable]
    public class ShopItemReactiveCollection : ReactiveCollection<ShopItem>
    {
    }
}
