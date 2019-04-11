using System;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
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

            player.Init(avatar.ToPlayer());

            return player.gameObject;
        }
    }
}
