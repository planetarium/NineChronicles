using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RuneLevelBonusEffectPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private RuneLevelBonusEffectScroll scroll;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                CloseWidget.Invoke();
                AudioController.PlayClick();
            });
            CloseWidget = () => Close();
        }

        public void Show()
        {
            var models = new List<RuneLevelBonusEffectCell.Model>();

            scroll.UpdateData(models, true);

            base.Show();
        }
    }
}
