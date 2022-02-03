using Coffee.UIEffects;
using Nekoyume.Game.Character;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class BaseItemView : MonoBehaviour
    {
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
        private GameObject expiredObject;

        [SerializeField]
        private GameObject disableObject;

        [SerializeField]
        private GameObject selectObject;

        [SerializeField]
        private GameObject selectEnchantItemObject;

        [SerializeField]
        private GameObject lockObject;

        [SerializeField]
        private GameObject shadowObject;

        public ItemViewDataScriptableObject ItemViewData => itemViewData;
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
        public GameObject ExpiredObject => expiredObject;
        public GameObject DisableObject => disableObject;
        public GameObject SelectObject => selectObject;
        public GameObject SelectEnchantItemObject => selectEnchantItemObject;
        public GameObject LockObject => lockObject;
        public GameObject ShadowObject => shadowObject;
    }
}
