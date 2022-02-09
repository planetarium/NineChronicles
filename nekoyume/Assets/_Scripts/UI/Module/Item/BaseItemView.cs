using System;
using System.Linq;
using Coffee.UIEffects;
using Nekoyume.Game.Character;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class BaseItemView : MonoBehaviour
    {
        [SerializeField]
        private GameObject container;

        [SerializeField]
        private ItemViewDataScriptableObject itemViewData;

        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        private Image gradeImage;

        [SerializeField]
        private UIHsvModifier gradeHsv;

        [SerializeField]
        private GameObject enoughObject;

        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Image enhancementImage;

        [SerializeField]
        private TextMeshProUGUI enhancementText;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private TextMeshProUGUI priceText;

        [SerializeField]
        private ItemOptionTag optionTag;

        [SerializeField]
        private GameObject notificationObject;

        [SerializeField]
        private GameObject equippedObject;

        [SerializeField]
        private TouchHandler minusTouchHandler;

        [SerializeField]
        private GameObject focusObject;

        [SerializeField]
        private GameObject expiredObject;

        [SerializeField]
        private GameObject disableObject;

        [SerializeField]
        private GameObject limitObject;

        [SerializeField]
        private GameObject selectObject;

        [SerializeField]
        private GameObject selectEnchantItemObject;

        [SerializeField]
        private GameObject lockObject;

        [SerializeField]
        private GameObject shadowObject;

        public GameObject Container => container;
        public TouchHandler TouchHandler => touchHandler;
        public Image GradeImage => gradeImage;
        public UIHsvModifier GradeHsv => gradeHsv;
        public GameObject EnoughObject => enoughObject;
        public Image ItemImage => itemImage;
        public Image EnhancementImage => enhancementImage;
        public TextMeshProUGUI EnhancementText => enhancementText;
        public TextMeshProUGUI CountText => countText;
        public TextMeshProUGUI PriceText => priceText;
        public ItemOptionTag OptionTag => optionTag;
        public GameObject NotificationObject => notificationObject;
        public GameObject EquippedObject => equippedObject;
        public TouchHandler MinusTouchHandler => minusTouchHandler;
        public GameObject FocusObject => focusObject;
        public GameObject ExpiredObject => expiredObject;
        public GameObject DisableObject => disableObject;
        public GameObject LimitObject => limitObject;
        public GameObject SelectObject => selectObject;
        public GameObject SelectEnchantItemObject => selectEnchantItemObject;
        public GameObject LockObject => lockObject;
        public GameObject ShadowObject => shadowObject;

        private ItemSheet.Row GetRow(ItemBase itemBase)
        {
            var sheet = Game.Game.instance.TableSheets;
            var row = sheet.ItemSheet.Values.FirstOrDefault(r => r.Id == itemBase.Id);

            if (row is null)
            {
                throw new ArgumentOutOfRangeException(nameof(ItemSheet.Row), itemBase.Id, null);
            }

            return row;
        }

        public Sprite GetItemIcon(ItemBase itemBase)
        {
            var row = GetRow(itemBase);
            var icon = SpriteHelper.GetItemIcon(row.Id);
            if (icon is null)
            {
                throw new FailedToLoadResourceException<Sprite>(row.Id.ToString());
            }

            return icon;
        }

        public ItemViewData GetItemViewData(ItemBase itemBase)
        {
            var row = GetRow(itemBase);
            var add = itemBase is TradableMaterial ? 1 : 0;
            return itemViewData.GetItemViewData(row.Grade + add);
        }
    }
}
