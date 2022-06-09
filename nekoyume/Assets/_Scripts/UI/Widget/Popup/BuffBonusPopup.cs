using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    public class BuffBonusPopup : PopupWidget
    {
        [SerializeField]
        private ConditionalCostButton normalButton = null;

        [SerializeField]
        private ConditionalCostButton advancedButton = null;

        private bool _hasEnoughStars;

        protected override void Awake()
        {
            
        }

        public void Show(int stageId, bool hasEnoughStars)
        {
            _hasEnoughStars = hasEnoughStars;
            var sheet = Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet;
            var normalCost = CrystalCalculator.CalculateBuffGachaCost(stageId, false, sheet);
            var advancedCost = CrystalCalculator.CalculateBuffGachaCost(stageId, true, sheet);
            normalButton.SetCost(CostType.Crystal, (long)normalCost.MajorUnit);
            normalButton.SetCondition(() => hasEnoughStars);
            normalButton.UpdateObjects();
            advancedButton.SetCost(CostType.Crystal, (long)advancedCost.MajorUnit);
            advancedButton.SetCondition(() => hasEnoughStars);
            normalButton.UpdateObjects();

            base.Show();
        }

        private void OnClickNormalButton()
        {

        }

        private void OnClickAdvancedButton()
        {

        }
    }
}
