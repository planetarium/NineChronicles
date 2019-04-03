using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class InventorySlot : MonoBehaviour
    {
        private static Sprite _defaultSprite = null;

        public Image Icon;
        public Text LabelCount;
        public Text LabelEquip;
        public bool toggled;
        public GameObject outLine;

        public ItemBase Item;

        #region Mono

        private void Awake()
        {
            _defaultSprite = Resources.Load<Sprite>("images/item_301001");
        }

        #endregion

        public void Clear()
        {
            Icon.gameObject.SetActive(false);
            LabelCount.text = "";
            LabelEquip.text = "";
        }

        public void Set(ItemBase item, int count)
        {
            Item = item;

            var sprite = Resources.Load<Sprite>($"images/item_{item.Data.Id}");
            if (sprite == null)
            {
                sprite = _defaultSprite;
            }

            Icon.sprite = sprite;
            Icon.SetNativeSize();
            Icon.gameObject.SetActive(true);

            LabelCount.text = count.ToString();
            LabelEquip.text = "";
            toggled = false;
            outLine.SetActive(toggled);
        }

        public void SlotClick()
        {
            toggled = !toggled;
            outLine.SetActive(toggled);
            Game.Event.OnSlotClick.Invoke(this, toggled);
        }

        public void SetAlpha(float alpha)
        {
            var color = Icon.color;
            color.a = alpha;
            Icon.color = color;
        }
    }
}
