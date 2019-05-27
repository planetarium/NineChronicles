using System;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.State;
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

            return Create(avatar.ToPlayer());
        }
        public GameObject Create(AvatarState avatarState)
        {
            if (ReferenceEquals(avatarState, null))
            {
                throw new ArgumentNullException("`Model.Avatar` can't be null.");
            }

            return Create(avatarState.ToPlayer());
        }

        public GameObject Create(Player model = null)
        {
            if (ReferenceEquals(model, null))
            {
                model = new Player();
            }

            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var player = objectPool.Get<Character.Player>();
            if (ReferenceEquals(player, null))
            {
                throw new NotFoundComponentException<Character.Player>();
            }

            player.Init(model);

            return player.gameObject;
        }
    }
}
