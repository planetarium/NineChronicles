using System.Collections.Generic;
using Nekoyume.Helper;
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

        public void SetData(List<Equipment> equipments, List<Costume> costumes, int turnLimit)
        {
            var portraitId = Util.GetPortraitId(equipments, costumes);
            characterView.SetByFullCostumeOrArmorId(portraitId);
            battleTimerView.Show(turnLimit);
        }

        public void UpdateTurnLimit(int turnLimit)
        {
            battleTimerView.UpdateTurnLimit(turnLimit);
        }
    }
}
