using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
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


        public void Init(Stats stats)
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
            _player = FindObjectOfType<Player>();
            foreach (var equipment in _player.equipments)
            {
                var type = (ItemBase.ItemType) Enum.Parse(typeof(ItemBase.ItemType), equipment.Data.Cls);
                foreach (var slot in equipSlots)
                {
                    var es = slot.GetComponent<EquipSlot>();
                    if (es.type == type)
                    {
                        es.Set(equipment);
                    }
                }
            }
            base.Show();
        }
    }
}
