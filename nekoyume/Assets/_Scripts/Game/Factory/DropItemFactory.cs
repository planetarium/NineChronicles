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
            Data.Table.Item itemData;
            if (!tables.Item.TryGetValue(itemId, out itemData))
                return null;

            var type = typeof(Item.ItemBase).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.IsDefined(typeof(Item.ItemBase), false) && itemData.Cls == name);
            var item = System.Activator.CreateInstance(type) as Item.ItemBase;
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
            if (sprite == null)
                sprite = Resources.Load<Sprite>("images/pet");
            render.sprite = sprite;
            render.sortingOrder = Mathf.FloorToInt(-position.y * 10.0f);

            return dropItem.gameObject;
        }
    }
}
