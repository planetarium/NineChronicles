using Nekoyume.Move;
using System.Collections.Generic;
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

        private void Awake()
        {
            _slots = new List<InventorySlot>();
            _slotBase.SetActive(true);
            for (int i = 0; i < 40; ++i)
            {
                GameObject newSlot = Instantiate(_slotBase, _grid);
                InventorySlot slot = newSlot.GetComponent<InventorySlot>();
                _slots.Add(slot);
            }
            _slotBase.SetActive(false);
        }

        public override void Show()
        {
            // FIX ME : get item list
            string itemsstr = MoveManager.Instance.Avatar.items;
            string[] items = new string[] {};

            for (int i = 0; i < 40; ++i)
            {
                InventorySlot slot = _slots[i];
                if (items != null && items.Length > i)
                {
                    slot.Set(items[i], 1);
                }
                else
                {
                    slot.Clear();
                }
            }

            base.Show();
        }
    }
}
