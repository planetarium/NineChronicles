using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Util;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public static class ItemMoveAnimationFactory
    {
        private static readonly Lazy<ObjectPool> ObjectPool = new Lazy<ObjectPool>(() =>
        {
            var pool = MainCanvas
                .instance
                .GetLayerRootTransform(WidgetType.Animation)
                .Find(AnimationPoolObjectName)
                .GetComponent<ObjectPool>();
            pool.Initialize();
            return pool;
        });

        public enum AnimationItemType
        {
            Crystal,
            Ncg,
        }

        private const string CrystalAnimationPrefabName = "item_CrystalGetAnimation";
        private const string AnimationPoolObjectName = "ItemMoveAnimationPool";

        public static IEnumerator CoItemMoveAnimation(AnimationItemType type, Vector3 startPosition, Vector3 endPosition, int count)
        {
            while (count-- > 0)
            {
                var anim = ObjectPool.Value.Get(type switch
                {
                    AnimationItemType.Crystal => CrystalAnimationPrefabName,
                    AnimationItemType.Ncg => "DummyNcg?",
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                }, true, startPosition);

                anim.GetComponent<ItemMoveAnimation>().Show(
                    startPosition,
                    endPosition,
                    Vector2.one,
                    true,
                    false,
                    setMidByRandom: true,
                    destroy: false);
            }

            yield return null;
        }
    }
}
