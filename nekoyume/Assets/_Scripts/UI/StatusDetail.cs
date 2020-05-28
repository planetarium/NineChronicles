using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    /// <summary>
    /// Fix me.
    /// Status 위젯과 함께 사용할 때에는 해당 위젯 하위에 포함되어야 함.
    /// 지금은 별도의 위젯으로 작동하는데, 이 때문에 위젯 라이프 사이클의 일관성을 잃음.(스스로 닫으면 안 되는 예외 발생)
    /// </summary>
    public class StatusDetail : XTweenWidget
    {
        public TextMeshProUGUI statusTitleText;
        public TextMeshProUGUI equipmentTitleText;
        public EquipmentSlots equipmentSlots;
        public GameObject textOption;
        public GameObject optionGroup;
        public Blur blur;
        public DetailedStatView[] statusRows;

        private Game.Character.Player _player;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            statusTitleText.text = LocalizationManager.Localize("UI_STATUS");
            equipmentTitleText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
        }

        protected override void OnDisable()
        {
            if (optionGroup != null)
            {
                foreach (Transform child in optionGroup.transform)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }

            Find<ItemInformationTooltip>().Close();
            base.OnDisable();
        }

        #endregion

        public override void Show(bool ignoreShowAnimation = false)
        {
            _player = Game.Game.instance.Stage.selectedPlayer;
            var player = _player.Model;

            equipmentSlots.SetPlayerEquipments(_player.Model, ShowTooltip, null);

            // status info
            var tuples = player.Stats.GetBaseAndAdditionalStats();
            int idx = 0;
            foreach (var (statType, value, additionalValue) in tuples)
            {
                var info = statusRows[idx];
                info.Show(statType, value, additionalValue);
                ++idx;
            }

            //option info
            foreach (var option in player.GetOptions())
            {
                GameObject go = Instantiate(textOption, optionGroup.transform);
                var text = go.GetComponent<Text>();
                text.text = option;
                go.SetActive(true);
            }

            base.Show(ignoreShowAnimation);

            if (blur)
            {
                blur.Show();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur)
            {
                blur.Close();
            }
            
            base.Close(ignoreCloseAnimation);
            equipmentSlots.Clear();
        }

        private void ShowTooltip(EquipmentSlot slot)
        {
            var tooltip = Find<ItemInformationTooltip>();

            if (slot is null ||
                slot.Item is null ||
                slot.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(slot.RectTransform, new CountableItem(slot.Item, 1));
        }

        public void CloseClick()
        {
            AudioController.PlayClick();
            Find<Status>()?.CloseStatusDetail();
        }
    }
}
