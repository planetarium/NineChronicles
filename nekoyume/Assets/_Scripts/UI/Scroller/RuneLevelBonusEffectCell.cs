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
            public int RewardMin;
            public int? RewardMax;
        }

        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private GameObject focusObject;

        public override void UpdateContent(Model itemData)
        {
            var levelBonusMaxText = itemData.LevelBonusMax.HasValue
                ? $"{itemData.LevelBonusMax.Value}"
                : string.Empty;
            bonusText.text = $"{itemData.LevelBonusMin} ~ {levelBonusMaxText}";
            var rewardMaxText = itemData.RewardMax.HasValue
                ? $"+{itemData.RewardMax / 1000m:0.###}%"
                : string.Empty;
            rewardText.text = $"+{itemData.RewardMin / 1000m:0.###}% ~ {rewardMaxText}";

            focusObject.SetActive(Context.CurrentModel == itemData);
        }
    }
}
