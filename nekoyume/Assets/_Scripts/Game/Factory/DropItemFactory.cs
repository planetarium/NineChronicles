using System.Linq;
using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.Factory
{
    public class DropItemFactory : MonoBehaviour
    {
        public GameObject Create(int itemId, Vector2 position)
        {
            Data.Tables tables = this.GetRootComponent<Data.Tables>();
            Item.ItemBase item = tables.GetItem(itemId);
            if (item == null)
                return null;

            var objectPool = GetComponent<Util.ObjectPool>();
            var dropItem = objectPool.Get<Item.DropItem>(position);
            if (dropItem == null)
                return null;

            dropItem.Item = item;

            // sprite
            var render = dropItem.GetComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>($"images/item_{itemId}");
            if (sprite != null)
                render.sprite = sprite;
            render.sortingOrder = 0;

            return dropItem.gameObject;
        }
    }
}
