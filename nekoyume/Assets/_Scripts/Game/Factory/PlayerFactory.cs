using System;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
        public static GameObject Create(AvatarState avatarState)
        {
            if (avatarState is null)
                throw new ArgumentNullException(nameof(avatarState));

            return Create(new Player(avatarState, Game.instance.TableSheets));
        }

        public static GameObject Create(Player model = null)
        {
            if (model is null)
            {
                model = new Player(1, Game.instance.TableSheets);
            }

            var objectPool = Game.instance.Stage.objectPool;
            var player = objectPool.Get<Character.Player>();
            if (!player)
                throw new NotFoundComponentException<Character.Player>();

            player.Set(model, true);
            return player.gameObject;
        }
    }
}
