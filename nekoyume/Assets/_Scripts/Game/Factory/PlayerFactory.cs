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

            // FIXME TableSheetsState.Current 써도 괜찮은지 체크해야 합니다.
            return Create(new Player(avatarState, TableSheets.FromTableSheetsState(TableSheetsState.Current)));
        }

        public static GameObject Create(Player model = null)
        {
            if (model is null)
                // FIXME TableSheetsState.Current 써도 괜찮은지 체크해야 합니다.
                model = new Player(1, TableSheets.FromTableSheetsState(TableSheetsState.Current));

            var objectPool = Game.instance.Stage.objectPool;
            var player = objectPool.Get<Character.Player>();
            if (!player)
                throw new NotFoundComponentException<Character.Player>();

            player.Set(model, true);
            return player.gameObject;
        }
    }
}
