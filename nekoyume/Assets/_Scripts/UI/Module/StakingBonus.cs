using System;
using System.Collections.Generic;
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

        [SerializeField]
        private List<Sprite> enableIcons;

        [SerializeField]
        private Image enableIcon;

        [SerializeField]
        private List<GameObject> enableObjects;

        private Func<int, string> _bonusTextFunc;

        private void Awake()
        {
            button.onClick.AddListener(() => Widget.Find<StakingPopup>().Show());
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
            stakingBonusText.gameObject.SetActive(bonusEnabled);
            if (bonusEnabled)
            {
                stakingLevelText.text = $"Staking Lv.{level}";
                stakingBonusText.text = _bonusTextFunc?.Invoke(level);
                enableIcon.sprite = enableIcons[level - 1];
                for(var i = 0; i < enableObjects.Count; i++)
                {
                    enableObjects[i].SetActive(i < level);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(stakingBonusText.rectTransform);
        }
    }
}
