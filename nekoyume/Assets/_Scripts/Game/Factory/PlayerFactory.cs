using System;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
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

            // FIXME TableSheetsState.Current 써도 괜찮은지 체크해야 합니다.
            return Create(new Player(avatarState, TableSheets.FromTableSheetsState(TableSheetsState.Current)));
        }

        public GameObject Create(Player model = null)
        {
            if (ReferenceEquals(model, null))
            {
                // FIXME TableSheetsState.Current 써도 괜찮은지 체크해야 합니다.
                model = new Player(1, TableSheets.FromTableSheetsState(TableSheetsState.Current));
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

            player.Set(model, true);

            return player.gameObject;
        }
    }
}
