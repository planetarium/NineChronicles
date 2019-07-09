using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Model;
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
        public EquipSlot[] equipSlots;
        public GameObject textOption;
        public GameObject group;
        public GameObject statusInfo;
        public GameObject optionGroup;

        private Game.Character.Player _player;
        
        #region Mono

        private void OnDisable()
        {
            if (group != null)
                foreach (Transform child in group.transform)
                {
                    Destroy(child.gameObject);
                }

            if (optionGroup != null)
                foreach (Transform child in optionGroup.transform)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
        }

        #endregion
        
        public override void Show()
        {
            _player = FindObjectOfType<Game.Character.Player>();
            var player = _player.model;

            // equip slot
            foreach (var equipment in _player.equipments)
            {
                var type = equipment.Data.cls.ToEnumItemType();
                var slot = FindSlot(type);
                if (slot)
                    slot.Set(equipment);
            }

            // status info
            var fields = player.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(InformationFieldAttribute), true))
                {
                    GameObject row = Instantiate(statusInfo, group.transform);
                    var info = row.GetComponent<StatusInfo>();
                    info.Set(field.Name, field.GetValue(player), player.GetAdditionalStatus(field.Name));
                }
            }

            //option info
            foreach (var option in player.GetOptions())
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

        private EquipSlot FindSlot(ItemBase.ItemType type)
        {
            if (type == ItemBase.ItemType.Ring)
            {
                return equipSlots.FirstOrDefault(es =>
                           es.type == ItemBase.ItemType.Ring && es.item?.Data is null)
                       ?? equipSlots.First(es => es.type == ItemBase.ItemType.Ring);
            }

            return equipSlots.FirstOrDefault(es => es.type == type);
        }
    }
}
