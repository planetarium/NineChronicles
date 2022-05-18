using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class StakingBonus : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text stakingLevelText;

        [SerializeField]
        private TMP_Text stakingBonusText;

        [SerializeField]
        private Button button;

        [SerializeField]
        private GameObject enableObject;

        [SerializeField]
        private GameObject disableObject;

        private Func<int, string> _bonusTextFunc;

        private void Awake()
        {
            button.onClick.AddListener(() => { Application.OpenURL(MaterialTooltip.StakingDescriptionUrl); });
        }

        public void SetBonusTextFunc(Func<int, string> bonusTextFunc)
        {
            _bonusTextFunc = bonusTextFunc;
        }

        public void OnUpdateStakingLevel(int level)
        {
            var bonusEnabled = level > 0;
            enableObject.SetActive(bonusEnabled);
            disableObject.SetActive(!bonusEnabled);
            stakingLevelText.text = $"Staking Lv.{level}";
            stakingBonusText.text = _bonusTextFunc?.Invoke(level);
        }
    }
}
