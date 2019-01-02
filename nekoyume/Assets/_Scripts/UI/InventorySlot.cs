using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class InventorySlot : MonoBehaviour
    {
        public Image Icon;
        public Text LabelCount;
        public Text LabelEquip;

        public ItemBase Item;

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(SlotClick);
        }

        public void Clear()
        {
            Icon.gameObject.SetActive(false);
            LabelCount.text = "";
            LabelEquip.text = "";
        }

        public void Set(ItemBase item, int count)
        {
            var sprite = Resources.Load<Sprite>($"images/item_{item.Data.Id}");
            Icon.sprite = sprite;
            Icon.gameObject.SetActive(true);
            Icon.SetNativeSize();
            LabelCount.text = count.ToString();
            LabelEquip.text = "";
            Item = item;
        }

        public void SlotClick()
        {
            if (Item != null && Item is Weapon)
            {
                Game.Event.OnEquip.Invoke((Equipment) Item);
            }
        }
    }
}
