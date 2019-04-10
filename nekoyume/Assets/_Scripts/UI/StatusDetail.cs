using Nekoyume.Data.Table;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StatusDetail : Widget
    {
        public Text textAtk;
        public Text textDef;
        public GameObject[] equipSlots;
        private Game.Character.Player _player;
        public GameObject textOption;
        public GameObject group;
        public GameObject statusInfo;
        public GameObject OptionGroup;

        public void Init(Level level)
        {
//            textAtk.text = stats.Attack.ToString();
//            textDef.text = stats.Defense.ToString();
        }

        public void CloseClick()
        {
            var status = Find<Status>();
            if (status)
            {
                status.BtnStatus.group.SetAllTogglesOff();
            }
            AudioController.PlayClick();

        }

        private void OnDisable()
        {
            if (group != null)
                foreach (Transform child in group.transform)
                {
                    Destroy(child.gameObject);
                }

            if (OptionGroup != null)
                foreach (Transform child in OptionGroup.transform)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
        }

        public override void Show()
        {
            _player = FindObjectOfType<Game.Character.Player>();
            var player = _player.model;

            // equip slot
            foreach (var equipment in _player.equipments)
            {
                var type = equipment.equipData.cls.ToEnumItemType();
                foreach (var slot in equipSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Set(equipment);
                    }
                }
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
                GameObject go = Instantiate(textOption, OptionGroup.transform);
                var text = go.GetComponent<Text>();
                text.text = option;
                go.SetActive(true);
            }

            base.Show();
        }
    }
}
