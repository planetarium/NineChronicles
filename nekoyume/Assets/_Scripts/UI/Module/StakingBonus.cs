using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.State.Subjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class StakingBonus : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text stakingLevelText;

        [SerializeField]
        private TMP_Text stakingBonusText;

        [SerializeField]
        private Button button;

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
            stakingLevelText.text = $"Staking Lv.{level}";
            stakingBonusText.text = _bonusTextFunc?.Invoke(level);
        }
    }
}
