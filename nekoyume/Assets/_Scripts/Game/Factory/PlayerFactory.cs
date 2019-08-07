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
        
        public GameObject Create(AvatarState avatarState)
        {
            if (ReferenceEquals(avatarState, null))
            {
                throw new ArgumentNullException("`Model.Avatar` can't be null.");
            }

            return Create(new Player(avatarState));
        }

        public GameObject Create(Player model = null)
        {
            if (ReferenceEquals(model, null))
            {
                model = new Player(1);
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
