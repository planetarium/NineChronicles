using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MobileShop : Widget
    {
        [SerializeField]
        private Button purchaseButton;

        protected override void Awake()
        {
            base.Awake();
            purchaseButton.onClick.AddListener(() =>
            {

            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }
    }
}
