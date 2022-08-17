using Nekoyume.Game.Character;
using Nekoyume.UI.Module.Timer;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RaidPlayerStatus : MonoBehaviour
    {
        [SerializeField]
        private FramedCharacterView characterView = null;

        [SerializeField]
        private BattleTimerView battleTimerView = null;

        private const int RaidTurnLimit = 150;

        public void SetData(Player player)
        {
            characterView.SetByPlayer(player);
            battleTimerView.Show(RaidTurnLimit);
        }
    }
}
