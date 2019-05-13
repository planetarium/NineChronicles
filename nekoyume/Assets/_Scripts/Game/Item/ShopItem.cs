using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ShopItem : IDisposable
    {
        public readonly ByteArrayReactiveProperty owner = new ByteArrayReactiveProperty();
        public readonly ItemBaseReactiveProperty item = new ItemBaseReactiveProperty();
        public readonly IntReactiveProperty count = new IntReactiveProperty();
        public readonly DecimalReactiveProperty price = new DecimalReactiveProperty();

        public virtual void Dispose()
        {
            owner.Dispose();
            item.Dispose();
            count.Dispose();
            price.Dispose();
        }
    }
}
