using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Inventory : Widget
    {
        [SerializeField]
        private Transform _grid;
        [SerializeField]
        private GameObject _slotBase;

        private List<InventorySlot> _slots;
        private Player _player;

        private void Awake()
        {
            _slots = new List<InventorySlot>();
            _slotBase.SetActive(true);
            for (int i = 0; i < 42; ++i)
            {
                GameObject newSlot = Instantiate(_slotBase, _grid);
                InventorySlot slot = newSlot.GetComponent<InventorySlot>();
                slot.Item = null;
                _slots.Add(slot);
            }
            _slotBase.SetActive(false);
            Game.Event.OnUpdateEquipment.AddListener(UpdateEquipment);
            Game.Event.OnSlotClick.AddListener(ToggleSlot);
        }

        private void ToggleSlot(InventorySlot selected)
        {
            foreach (var slot in _slots)
            {
                if (slot != selected && slot.outLine != null)
                {
                    slot.toggled = false;
                    slot.outLine.SetActive(false);
                }
            }
        }

        private void UpdateEquipment(Equipment equipment)
        {
            foreach (var slot in _slots)
            {
                var item = slot.Item;
                if (item != null && item is Weapon)
                {
                    slot.LabelEquip.text = "";
                    if (item == equipment)
                    {
                        slot.LabelEquip.text = equipment.IsEquipped ? "E" : "";
                    }
                }
            }
        }

        public override void Show()
        {
            _player = FindObjectOfType<Player>();
            // FIX ME : get item list
            if (_player != null)
            {
                List<Game.Item.Inventory.InventoryItem> items = _player.Inventory.items;
                for (int i = 0; i < 40; ++i)
                {
                    InventorySlot slot = _slots[i];
                    if (items != null && items.Count > i)
                    {
                        var inventoryItem = items[i];
                        slot.Set(inventoryItem.Item, inventoryItem.Count);
                        if (inventoryItem.Item is Weapon)
                        {
                            var weapon = (Weapon) inventoryItem.Item;
                            slot.LabelEquip.text = weapon.IsEquipped ? "E" : "";
                        }
                    }
                    else
                    {
                        slot.Clear();
                    }
                }
            }

            base.Show();
        }

        public void CloseClick()
        {
            var status = Widget.Find<Status>();
            if (status)
            {
                status.BtnInventory.group.SetAllTogglesOff();
            }
        }

    }
}
