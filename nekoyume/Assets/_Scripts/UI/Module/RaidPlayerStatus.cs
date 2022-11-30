using System.Collections.Generic;
using Nekoyume.Model.Item;
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

        public void SetData(List<Equipment> equipments, List<Costume> costumes, int characterId, int turnLimit)
        {
            characterView.Set(equipments, costumes, characterId);
            battleTimerView.Show(turnLimit);
        }

        public void UpdateTurnLimit(int turnLimit)
        {
            battleTimerView.UpdateTurnLimit(turnLimit);
        }
    }
}
