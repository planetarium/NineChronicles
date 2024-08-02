using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GrindRewardPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private GrindRewardScroll scroll;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close());
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            // Todo : Implement
            var models = new GrindRewardCell.Model[5];

            scroll.UpdateData(models);
        }
    }
}
