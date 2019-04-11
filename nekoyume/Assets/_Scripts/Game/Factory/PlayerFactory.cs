using System;
using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
        private const int DefaultSetId = 101000;
        public GameObject Create(Model.Avatar avatar)
        {
            if (ReferenceEquals(avatar, null))
            {
                throw new ArgumentNullException("`Model.Avatar` can't be null.");
            }

            var objectPool = GetComponent<ObjectPool>();
            var player = objectPool.Get<Player>();
            if (ReferenceEquals(player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            var go = player.gameObject;

            var prevAnim = go.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }

            var model = avatar.ToPlayer();
            var origin = Resources.Load<GameObject>($"Prefab/{model.set?.Data.id}") ??
                         Resources.Load<GameObject>($"Prefab/{DefaultSetId}");

            Instantiate(origin, go.transform);
            player.Init(avatar.ToPlayer());

            return go;
        }
    }
}
