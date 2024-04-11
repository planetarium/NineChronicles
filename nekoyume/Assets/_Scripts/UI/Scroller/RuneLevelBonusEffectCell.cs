using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneLevelBonusEffectCell : RectCell<RuneLevelBonusEffectCell.Model, RuneLevelBonusEffectScroll.ContextModel>
    {
        public class Model
        {
            public int LevelBonusMin;
            public int? LevelBonusMax;
            public int Reward;
        }

        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private GameObject focusObject;

        public override void UpdateContent(Model itemData)
        {
            var maxText = itemData.LevelBonusMax.HasValue
                ? $"{itemData.LevelBonusMax.Value}"
                : string.Empty;
            bonusText.text = $"{itemData.LevelBonusMin} ~ {maxText}";
            rewardText.text = $"{itemData.Reward / 100f:0.####}%";

            focusObject.SetActive(Context.CurrentModel == itemData);
        }
    }
}
