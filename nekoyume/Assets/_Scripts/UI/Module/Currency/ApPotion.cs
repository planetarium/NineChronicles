using Nekoyume.Helper;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ApPotion : AlphaAnimateModule
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI count;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private Button button;

        public Image IconImage => iconImage;

        private void Awake()
        {
            button.onClick.AddListener(ShowMaterialNavigationPopup);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateApPotion();
        }

        private void UpdateApPotion()
        {
            if(Game.Game.instance.States.CurrentAvatarState == null)
            {
                return;
            }
            if(Game.Game.instance.States.CurrentAvatarState.inventory == null)
            {
                return;
            }
            count.text = Game.Game.instance.States.CurrentAvatarState.inventory.GetMaterialCount((int)CostType.ApPotion).ToString();
        }

        public void SetActiveLoading(bool value)
        {
            loadingObject.SetActive(value);
            count.gameObject.SetActive(!value);
        }

        private void ShowMaterialNavigationPopup()
        {
            //todo: implement this
            //Widget.Find<MaterialNavigationPopup>().ShowCurrency((int)CurrencyType.ApPotion);
        }
    }
}
