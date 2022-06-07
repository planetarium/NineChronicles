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
        private GameObject emptyObject;

        [SerializeField]
        private ItemViewDataScriptableObject itemViewData;

        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        private TouchHandler minusTouchHandler;

        [SerializeField]
        private Image gradeImage;

        [SerializeField]
        private UIHsvModifier gradeHsv;

        [SerializeField]
        private GameObject enoughObject;

        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Image spineItemImage;

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
        private GameObject minusObject;

        [SerializeField]
        private GameObject focusObject;

        [SerializeField]
        private GameObject expiredObject;

        [SerializeField]
        private GameObject tradableObject;

        [SerializeField]
        private GameObject dimObject;

        [SerializeField]
        private GameObject levelLimitObject;

        [SerializeField]
        private GameObject selectObject;

        [SerializeField]
        private GameObject selectBaseItemObject;

        [SerializeField]
        private GameObject selectMaterialItemObject;

        [SerializeField]
        private GameObject lockObject;

        [SerializeField]
        private GameObject shadowObject;

        [SerializeField]
        private ParticleSystem itemGradeParticle;

        [SerializeField]
        private GameObject grindingCountObject;

        [SerializeField]
        private TMP_Text grindingCountText;

        public GameObject Container => container;
        public GameObject EmptyObject => emptyObject;
        public TouchHandler TouchHandler => touchHandler;
        public TouchHandler MinusTouchHandler => minusTouchHandler;
        public Image GradeImage => gradeImage;
        public UIHsvModifier GradeHsv => gradeHsv;
        public GameObject EnoughObject => enoughObject;
        public Image ItemImage => itemImage;
        public Image SpineItemImage => spineItemImage;
        public Image EnhancementImage => enhancementImage;
        public TextMeshProUGUI EnhancementText => enhancementText;
        public TextMeshProUGUI CountText => countText;
        public TextMeshProUGUI PriceText => priceText;
        public ItemOptionTag OptionTag => optionTag;
        public GameObject NotificationObject => notificationObject;
        public GameObject EquippedObject => equippedObject;
        public GameObject MinusObject => minusObject;
        public GameObject FocusObject => focusObject;
        public GameObject ExpiredObject => expiredObject;
        public GameObject TradableObject => tradableObject;
        public GameObject DimObject => dimObject;
        public GameObject LevelLimitObject => levelLimitObject;
        public GameObject SelectObject => selectObject;
        public GameObject SelectBaseItemObject => selectBaseItemObject;
        public GameObject SelectMaterialItemObject => selectMaterialItemObject;
        public GameObject LockObject => lockObject;
        public GameObject ShadowObject => shadowObject;
        public ParticleSystem ItemGradeParticle => itemGradeParticle;
        public GameObject GrindingCountObject => grindingCountObject;
        public TMP_Text GrindingCountText => grindingCountText;

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
