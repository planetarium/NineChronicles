using System.Collections;
using System.Collections.Generic;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class DropItemFactory : MonoBehaviour
    {
        private ItemBase _box;
        private Tables _tables;
        private const int BoxId = 100000;

        private void Start()
        {
            _tables = this.GetRootComponent<Tables>();
            _box = _tables.GetItem(BoxId);
        }
        public IEnumerator CoCreate(List<ItemBase> items, Vector3 position)
        {
            Tables tables = this.GetRootComponent<Tables>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                ItemBase exist = tables.GetItem(item.Data.id);
                if (exist == null)
                {
                    items.Remove(item);
                }
            }

            var objectPool = GetComponent<ObjectPool>();
            var dropItem = objectPool.Get<DropItem>(position);
            if (dropItem == null)
                yield break;


            // sprite
            var render = dropItem.GetComponent<SpriteRenderer>();
            var sprite = ItemBase.GetSprite(_box);
            render.sprite = sprite;
            render.sortingOrder = 0;

            yield return dropItem.CoSet(items);
        }
    }
}
