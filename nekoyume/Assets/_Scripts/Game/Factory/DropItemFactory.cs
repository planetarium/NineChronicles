using System.Collections;
using System.Collections.Generic;
using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Game.Util;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class DropItemFactory : MonoBehaviour
    {
        private ItemBase _box;
        private const int BoxId = 100000;

        public void Initialize()
        {
            _box = ItemFactory.CreateMaterial(BoxId);
        }
        
        public IEnumerator CoCreate(List<ItemBase> items, Vector3 position)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var exist = ItemFactory.CreateMaterial(item.Data.Id);
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
            var sprite = _box.GetIconSprite();
            render.sprite = sprite;
            render.sortingOrder = 0;

            yield return dropItem.CoSet(items);
        }
    }
}
