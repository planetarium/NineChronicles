using Coffee.UIEffects;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume
{
    using Lib9c;
    using Libplanet.Types.Assets;
    using UniRx;
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
        private GameObject loadingObject;

        [SerializeField]
        private ParticleSystem itemGradeParticle;

        [SerializeField]
        private GameObject grindingCountObject;

        [SerializeField]
        private GameObject runeNotificationObj;

        [SerializeField]
        private GameObject runeSelectMove;

        [SerializeField]
        private GameObject selectCollectionObject;

        [SerializeField]
        private GameObject selectArrowObject;

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
        // TODO: 소유하지 않은 장비가 Tradable = true로 설정되어 있음. 네이밍이 꼬인것으로 추정되며 아이템 상태 개선이 필요해보임
        public GameObject TradableObject => tradableObject;
        public GameObject DimObject => dimObject;
        public GameObject LevelLimitObject => levelLimitObject;
        public GameObject SelectObject => selectObject;
        public GameObject SelectBaseItemObject => selectBaseItemObject;
        public GameObject SelectMaterialItemObject => selectMaterialItemObject;
        public GameObject LockObject => lockObject;
        public GameObject ShadowObject => shadowObject;
        public GameObject LoadingObject => loadingObject;
        public ParticleSystem ItemGradeParticle => itemGradeParticle;
        public GameObject GrindingCountObject => grindingCountObject;
        public GameObject RewardReceived;

        public GameObject RuneNotificationObj => runeNotificationObj;
        public GameObject RuneSelectMove => runeSelectMove;
        public GameObject SelectCollectionObject => selectCollectionObject;
        public GameObject SelectArrowObject => selectArrowObject;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public static Sprite GetItemIcon(ItemBase itemBase)
        {
            var icon = itemBase.GetIconSprite();
            if (icon is null)
            {
                throw new FailedToLoadResourceException<Sprite>(itemBase.Id.ToString());
            }

            return icon;
        }

        public ItemViewData GetItemViewData(ItemBase itemBase)
        {
            var add = itemBase is TradableMaterial ? 1 : 0;
            return itemViewData.GetItemViewData(itemBase.Grade + add);
        }

        public ItemViewData GetItemViewData(int grade)
        {
            return itemViewData.GetItemViewData(grade);
        }

        protected void ClearItem()
        {
            Container.SetActive(true);
            EmptyObject.SetActive(false);
            EnoughObject.SetActive(false);
            MinusObject.SetActive(false);
            ExpiredObject.SetActive(false);
            SelectBaseItemObject.SetActive(false);
            SelectMaterialItemObject.SetActive(false);
            LockObject.SetActive(false);
            ShadowObject.SetActive(false);
            PriceText.gameObject.SetActive(false);
            LoadingObject.SetActive(false);
            EquippedObject.SetActive(false);
            DimObject.SetActive(false);
            TradableObject.SetActive(false);
            SelectObject.SetActive(false);
            FocusObject.SetActive(false);
            NotificationObject.SetActive(false);
            GrindingCountObject.SetActive((false));
            LevelLimitObject.SetActive(false);
            RewardReceived.SetActive(false);
            LevelLimitObject.SetActive(false);
            RuneNotificationObj.SetActiveSafe(false);
        }

        public void ItemViewSetCurrencyData(string ticker, decimal amount, FungibleAssetValue? fungibleAsset = null)
        {
            gameObject.SetActive(true);
            ClearItem();
            ItemImage.overrideSprite = SpriteHelper.GetFavIcon(ticker);
            if(fungibleAsset == null)
            {
                CountText.text = ((BigInteger)amount).ToCurrencyNotation();
            }
            else
            {
                CountText.text = fungibleAsset.Value.GetQuantityString();
            }
            GradeImage.sprite = SpriteHelper.GetItemBackground(Util.GetTickerGrade(ticker));
        }

        public bool ItemViewSetCurrencyData(int favId, decimal amount)
        {
            RuneSheet runeSheet = Game.Game.instance.TableSheets.RuneSheet;
            runeSheet.TryGetValue(favId, out var runeRow);
            if (runeRow != null)
            {
                ItemViewSetCurrencyData(runeRow.Ticker, amount);
                _disposables.DisposeAllAndClear();
                touchHandler.OnClick.Subscribe(_ =>
                {
                    Widget.Find<FungibleAssetTooltip>().Show(runeRow.Ticker, ((BigInteger)amount).ToCurrencyNotation(), null);
                }).AddTo(_disposables);
                return true;
            }

            NcDebug.LogWarning($"[ItemViewSetCurrencyData] Can't Find Fav ID {favId} in RuneSheet");
            switch (favId)
            {
                case 9999999:
                    ItemViewSetCurrencyData("NCG", amount);
                    return true;
                case 9999998:
                    ItemViewSetCurrencyData(Currencies.Crystal.Ticker, amount);
                    return true;
                case 9999997:
                    ItemViewSetCurrencyData("HOURGLASS", amount);
                    return true;
            }
            NcDebug.LogError($"[ItemViewSetCurrencyData] Can't Find Fav ID {favId} in RuneSheet");
            gameObject.SetActive(false);
            return false;
        }

        private void AddSimpleTooltip(int id, decimal amount)
        {
            _disposables.DisposeAllAndClear();
            if (!TableSheets.Instance.ItemSheet.TryGetValue(id, out var itemRow))
            {
                NcDebug.LogWarning($"Can't Find Item ID {id} in ItemSheet");
                return;
            }
            ItemBase itemBase = null;
            if (itemRow is MaterialItemSheet.Row materialRow)
            {
                itemBase = ItemFactory.CreateMaterial(materialRow);
            }
            else
            {
                for (var i = 0; i < amount; i++)
                {
                    if (itemRow.ItemSubType != ItemSubType.Aura)
                    {
                        itemBase = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                    }
                }
            }
            if (itemBase != null)
            {
                touchHandler.OnClick.Subscribe(_ =>
                {
                    var tooltip = ItemTooltip.Find(itemBase.ItemType);
                    tooltip.Show(itemBase, string.Empty, false, null);
                }).AddTo(_disposables);
            }
        }

        public void ItemViewSetItemData(int itemId, int amount)
        {
            gameObject.SetActive(true);
            ClearItem();
            AddSimpleTooltip(itemId, amount);
            ItemImage.overrideSprite = SpriteHelper.GetItemIcon(itemId);
            CountText.text = $"x{amount}";
            try
            {
                var itemSheetData = Game.Game.instance.TableSheets.ItemSheet[itemId];
                GradeImage.sprite = SpriteHelper.GetItemBackground(itemSheetData.Grade);
            }
            catch
            {
                NcDebug.LogError($"Can't Find Item ID {itemId} in ItemSheet");
            }
        }
    }
}
