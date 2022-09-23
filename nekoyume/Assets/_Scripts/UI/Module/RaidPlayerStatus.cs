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

        public void SetData(Player player, int turnLimit)
        {
            characterView.SetByPlayer(player);
            battleTimerView.Show(turnLimit);
        }

        public void UpdateTurnLimit(int turnLimit)
        {
            battleTimerView.UpdateTurnLimit(turnLimit);
        }
    }
}
