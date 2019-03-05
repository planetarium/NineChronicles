using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class PlayerFactory : MonoBehaviour
    {
        public GameObject Create()
        {
            var log = ActionManager.Instance.battleLog;
            var avatar = ActionManager.Instance.Avatar;
            if (avatar == null)
                return null;
            var objectPool = GetComponent<ObjectPool>();
            var player = objectPool.Get<Player>();
            if (player == null)
                return null;
            player.Init(avatar.ToPlayer());

            return player.gameObject;
        }
    }
}
