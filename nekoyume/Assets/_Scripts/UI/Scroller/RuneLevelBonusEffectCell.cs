using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneLevelBonusEffectCell : RectCell<RuneLevelBonusEffectCell.Model, RuneLevelBonusEffectScroll.ContextModel>
    {
        public class Model
        {
        }

        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private GameObject focusObject;

        public override void UpdateContent(Model itemData)
        {
        }
    }
}
