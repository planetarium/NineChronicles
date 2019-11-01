using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    /// <summary>
    /// Fix me.
    /// Status 위젯과 함께 사용할 때에는 해당 위젯 하위에 포함되어야 함.
    /// 지금은 별도의 위젯으로 작동하는데, 이 때문에 위젯 라이프 사이클의 일관성을 잃음.(스스로 닫으면 안 되는 예외 발생)
    /// </summary>
    public class StatusDetail : Widget
    {
        public Text statusTitleText;
        public Text equipmentTitleText;
        public EquipmentSlots equipmentSlots;
        public GameObject textOption;
        public GameObject group;
        public GameObject statusInfo;
        public GameObject optionGroup;

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
            if (group != null)
            {
                foreach (Transform child in group.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            if (optionGroup != null)
            {
                foreach (Transform child in optionGroup.transform)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }
            
            base.OnDisable();
        }

        #endregion

        public override void Show()
        {
            _player = FindObjectOfType<Game.Character.Player>();
            var player = _player.Model;

            // equip slot
            if (equipmentSlots is null)
                throw new NotFoundComponentException<EquipmentSlots>();

            foreach (var equipment in _player.Equipments)
            {
                if (equipmentSlots.TryGet(equipment.Data.ItemSubType, out var slot))
                    slot.Set(equipment);
            }

            // status info
            var tuples = player.Value.GetStatTuples();
            foreach (var (key, value, additional) in tuples)
            {
                var go = Instantiate(statusInfo, group.transform);
                var info = go.GetComponent<StatusInfo>();
                info.Set(key, value, additional);
            }


            //option info
            foreach (var option in player.Value.GetOptions())
            {
                GameObject go = Instantiate(textOption, optionGroup.transform);
                var text = go.GetComponent<Text>();
                text.text = option;
                go.SetActive(true);
            }

            base.Show();
        }

        public void CloseClick()
        {
            AudioController.PlayClick();
            Find<Status>()?.CloseStatusDetail();
        }
    }
}
