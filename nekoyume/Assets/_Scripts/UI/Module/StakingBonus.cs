using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.State.Subjects;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class StakingBonus : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text stakingLevelText;

        [SerializeField]
        private TMP_Text stakingBonusText;

        private Func<int, string> _bonusTextFunc;

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
