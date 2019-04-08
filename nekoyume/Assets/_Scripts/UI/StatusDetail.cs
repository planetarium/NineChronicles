using System.Text;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StatusDetail : Widget
    {
        public Text textAtk;
        public Text textDef;
        public GameObject[] equipSlots;
        private Player _player;
        public Text textOption;


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
        }

        public override void Show()
        {
            var builder = new StringBuilder();
            _player = FindObjectOfType<Player>();
            foreach (var equipment in _player.equipments)
            {
                var type = equipment.Data.cls.ToEnumItemType();
                foreach (var slot in equipSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Set(equipment);
                    }
                }
                builder.AppendLine($"{equipment.ToItemInfo()}");
            }

            textOption.text = builder.ToString();
            base.Show();
        }
    }
}
