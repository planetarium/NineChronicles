using System;
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

        // TODO: Use ReactiveProperty?
        public void UpdateApPotion()
        {
            if (Game.Game.instance.States.CurrentAvatarState == null)
            {
                return;
            }

            if (Game.Game.instance.States.CurrentAvatarState.inventory == null)
            {
                return;
            }

            var inventory = Game.Game.instance.States.CurrentAvatarState.inventory;
            var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;

            count.text = TextHelper.FormatNumber(inventory.GetUsableItemCount(CostType.ApPotion, blockIndex));
        }

        public void SetActiveLoading(bool value)
        {
            loadingObject.SetActive(value);
            count.gameObject.SetActive(!value);
        }

        private void ShowMaterialNavigationPopup()
        {
            Widget.Find<MaterialNavigationPopup>().ShowCurrency(CostType.ApPotion);
        }
    }
}
