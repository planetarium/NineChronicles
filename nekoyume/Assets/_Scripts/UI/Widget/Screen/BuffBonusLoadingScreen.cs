using System;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BuffBonusLoadingScreen : ScreenWidget
    {
        [SerializeField]
        private TMP_Text messageText;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Show(string message, bool ignoreShowAnimation = false)
        {

            base.Show(ignoreShowAnimation);
        }

        protected override void OnEnable()
        {

            base.OnEnable();
        }
    }
}
