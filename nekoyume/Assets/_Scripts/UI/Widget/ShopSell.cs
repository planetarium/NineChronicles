using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Libplanet.Types.Assets;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using Inventory = Nekoyume.UI.Module.Inventory;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    using Nekoyume.UI.Scroller;
    using UniRx;

    public class ShopSell : Widget
    {
        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private SellView view;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        [SerializeField]
        private Button reregistrationButton;

        [SerializeField]
        private Button cancelRegistrationButton;

        [SerializeField]
        private Button buyButton = null;

        [SerializeField]
        private Button closeButton = null;

        private const int LimitPrice = 100000000;

        private Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;

            reregistrationButton.onClick.AddListener(() =>
            {
                Find<CostTwoButtonPopup>().Show(
                    L10nManager.Localize("UI_SHOP_UPDATESELLALL_POPUP"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    CostType.ActionPoint, 5,
                    state => SubscribeConditionalButtonForChargeAp(state, "UI_SHOP_UPDATESELLALL",
                        SubscribeReRegisterProduct));
            });
            cancelRegistrationButton.onClick.AddListener(() =>
            {
                Find<CostTwoButtonPopup>().Show(
                    L10nManager.Localize("UI_SHOP_CANCELLATIONALL_POPUP"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    CostType.ActionPoint, 5,
                    state => SubscribeConditionalButtonForChargeAp(state, "UI_SHOP_CANCELLATIONALL",
                        SubscribeCancelProductRegistration));
            });

            buyButton.onClick.AddListener(() =>
            {
                speechBubble.gameObject.SetActive(false);
                Find<TwoButtonSystem>().Close();
                Find<ItemCountableAndPricePopup>().Close();
                Find<ShopBuy>().gameObject.SetActive(true);
                Find<ShopBuy>().Open();
                gameObject.SetActive(false);
            });

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            });

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            SharedModel = new Shop();
            SharedModel.ItemCountableAndPricePopup.Value.Item
                .Subscribe(SubscribeSellPopup)
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnClickConditional
                .Subscribe(tuple =>
                {
                    var (state, data) = tuple;
                    SubscribeConditionalButtonForChargeAp(state, "UI_SELL", chargeAp =>
                    {
                        data.ChargeAp.Value = chargeAp;
                        SubscribeRegisterProduct(data);
                    });
                })
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnClickReregister
                .Subscribe(SubscribeReRegisterProduct)
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnClickCancel
                .Subscribe(SubscribeSellPopupCancel)
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnChangeCount
                .Subscribe(SubscribeSellPopupCount)
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnChangePrice
                .Subscribe(SubscribeSellPopupPrice)
                .AddTo(gameObject);

            // sell cancellation
            SharedModel.ItemCountAndPricePopup.Value.Item
                .Subscribe(SubscribeSellCancellationPopup)
                .AddTo(gameObject);
        }

        private void ShowItemTooltip(InventoryItem model)
        {
            if (model.ItemBase is not null)
            {
                var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
                tooltip.Show(
                    model,
                    L10nManager.Localize("UI_SELL"),
                    model.ItemBase is ITradableItem,
                    () => ShowSell(model),
                    inventory.ClearSelectedItem,
                    () => L10nManager.Localize("UI_UNTRADABLE"));
            }
            else
            {
                Find<FungibleAssetTooltip>().Show(model,
                    () => ShowSell(model),
                    ()=>
                    {
                        inventory.ClearSelectedItem();
                        view.ClearSelectedItem();
                    });
            }
        }

        private void ShowSellTooltip(ShopItem model)
        {
            var inventoryItems = States.Instance.CurrentAvatarState.inventory.Items;
            var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
            var apStoneCount = inventoryItems.Where(x =>
                    x.item.ItemSubType == ItemSubType.ApStone &&
                    !x.Locked &&
                    !(x.item is ITradableItem tradableItem &&
                      tradableItem.RequiredBlockIndex > blockIndex))
                .Sum(item => item.count);

            if (model.ItemBase is not null)
            {
                var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
                tooltip.Show(model, apStoneCount,
                    state => SubscribeConditionalButtonForChargeAp(state, "UI_RETRIEVE",
                        chargeAp => ShowReRegisterProductPopup(model, chargeAp), apStoneCount),
                    state => SubscribeConditionalButtonForChargeAp(state, "UI_REREGISTER",
                        chargeAp => ShowRetrievePopup(model, chargeAp), apStoneCount),
                    view.ClearSelectedItem);
            }
            else
            {
                Find<FungibleAssetTooltip>().Show(model, apStoneCount,
                    state => SubscribeConditionalButtonForChargeAp(state, "UI_RETRIEVE",
                        chargeAp => ShowReRegisterProductPopup(model, chargeAp), apStoneCount),
                    state => SubscribeConditionalButtonForChargeAp(state, "UI_REREGISTER",
                        chargeAp => ShowRetrievePopup(model, chargeAp), apStoneCount),
                    view.ClearSelectedItem);
            }
        }
        private static void SubscribeConditionalButtonForChargeAp(ConditionalButton.State state,
            string key, Action<bool> action, int? apStoneCount = null)
        {
            if (apStoneCount == null)
            {
                var inventoryItems = States.Instance.CurrentAvatarState.inventory.Items;
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                apStoneCount = inventoryItems.Where(x =>
                        x.item.ItemSubType == ItemSubType.ApStone &&
                        !x.Locked &&
                        !(x.item is ITradableItem tradableItem &&
                          tradableItem.RequiredBlockIndex > blockIndex))
                    .Sum(item => item.count);
            }

            switch (state)
            {
                case ConditionalButton.State.Normal:
                    AudioController.PlayClick();
                    action(false);
                    break;
                case ConditionalButton.State.Conditional:
                    if (apStoneCount <= 0)
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("ERROR_ACTION_POINT"),
                            NotificationCell.NotificationType.Alert);
                        break;
                    }

                    var confirm = Find<IconAndButtonSystem>();
                    confirm.ShowWithTwoButton(L10nManager.Localize("UI_CONFIRM"),
                        L10nManager.Localize("UI_APREFILL_GUIDE_FORMAT",
                            L10nManager.Localize(key), apStoneCount),
                        L10nManager.Localize("UI_OK"),
                        L10nManager.Localize("UI_CANCEL"),
                        false, IconAndButtonSystem.SystemType.Information);
                    confirm.ConfirmCallback = () => action(true);
                    break;
                case ConditionalButton.State.Disabled:
                    break;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
#if UNITY_ANDROID || UNITY_IOS
            Find<MobileShop>().Show();
#else
            ShowAsync(ignoreShowAnimation);
#endif
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            inventory.SetShop(ShowItemTooltip);
            await ReactiveShopState.RequestSellProductsAsync();
            base.Show(ignoreShowAnimation);
            view.Show(
                ReactiveShopState.SellItemProducts,
                ReactiveShopState.SellFungibleAssetProducts,
                ShowSellTooltip);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            UpdateSpeechBubble();
        }

        private void UpdateSpeechBubble()
        {
            speechBubble.gameObject.SetActive(true);
            speechBubble.SetKey("SPEECH_SHOP_GREETING_");
            StartCoroutine(speechBubble.CoShowText(true));
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<TwoButtonSystem>().Close();
            Find<ItemCountableAndPricePopup>().Close();
            speechBubble.gameObject.SetActive(false);
            Find<ShopBuy>().Close();
            base.Close(ignoreCloseAnimation);
        }

        public void Close(bool ignoreOnRoomEnter, bool ignoreCloseAnimation)
        {
            Find<ItemCountAndPricePopup>().Close(ignoreCloseAnimation);
            if (!ignoreOnRoomEnter)
            {
                Game.Event.OnRoomEnter.Invoke(true);
            }

            base.Close(ignoreCloseAnimation);
        }

        public void UpdateInventory()
        {
            inventory.UpdateFungibleAssets();
        }

        private void ShowSell(InventoryItem model)
        {
            if (model is null)
            {
                return;
            }

            var data = SharedModel.ItemCountableAndPricePopup.Value;
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            data.Price.Value = new FungibleAssetValue(currency, Shop.MinimumPrice, 0);
            data.UnitPrice.Value = new FungibleAssetValue(currency, Shop.MinimumPrice, 0);
            data.Count.Value = 1;
            data.IsSell.Value = true;
            data.InfoText.Value = string.Empty;
            data.CountEnabled.Value = true;
            data.ChargeAp.Value = false;  // 나중에 버튼 누르면

            if (model.ItemBase is not null)
            {
                data.TitleText.Value = model.ItemBase.GetLocalizedName();
                data.Submittable.Value = !DimmedFuncForSell(model.ItemBase);
                data.Item.Value = new CountEditableItem(model.ItemBase,
                    1,
                    1,
                    model.Count.Value);
            }
            else
            {
                data.TitleText.Value = model.FungibleAssetValue.GetLocalizedName();
                data.Submittable.Value = true;
                data.Item.Value = new CountEditableItem(model.FungibleAssetValue,
                    1,
                    1,
                    model.Count.Value);
            }

            data.Item.Value.CountEnabled.Value = false;
        }

        private void ShowReRegisterProductPopup(ShopItem model, bool chargeAp) // 판매 갱신
        {
            var data = SharedModel.ItemCountableAndPricePopup.Value;
            data.IsSell.Value = false;
            data.InfoText.Value = string.Empty;
            data.CountEnabled.Value = true;
            data.ChargeAp.Value = chargeAp;

            if (model.Product is not null)
            {
                var price = model.Product.Price;
                var unitPrice = price / model.Product.Quantity;
                var majorUnit = (int)unitPrice;
                var minorUnit = (int)((unitPrice - majorUnit) * 100);
                var currency = States.Instance.GoldBalanceState.Gold.Currency;
                data.UnitPrice.Value = new FungibleAssetValue(currency, majorUnit, minorUnit);

                data.ProductId.Value = model.Product.ProductId;
                data.PrePrice.Value = (BigInteger)model.Product.Price * currency;
                data.Price.Value = (BigInteger)model.Product.Price * currency;
                var itemCount = (int)model.Product.Quantity;
                data.Count.Value = itemCount;

                data.TitleText.Value = model.ItemBase.GetLocalizedName();
                data.Submittable.Value = !DimmedFuncForSell(model.ItemBase);
                data.Item.Value = new CountEditableItem(model.ItemBase,
                    itemCount,
                    itemCount,
                    itemCount);
                data.Item.Value.CountEnabled.Value = false;
            }

            if (model.FungibleAssetProduct is not null)
            {
                var price = model.FungibleAssetProduct.Price;
                var unitPrice = price / model.FungibleAssetProduct.Quantity;
                var majorUnit = (int)unitPrice;
                var minorUnit = (int)((unitPrice - majorUnit) * 100);
                var currency = States.Instance.GoldBalanceState.Gold.Currency;
                data.UnitPrice.Value = new FungibleAssetValue(currency, majorUnit, minorUnit);

                data.ProductId.Value = model.FungibleAssetProduct.ProductId;
                data.PrePrice.Value = (BigInteger)model.FungibleAssetProduct.Price * currency;
                data.Price.Value = (BigInteger)model.FungibleAssetProduct.Price * currency;
                var itemCount = (int)model.FungibleAssetProduct.Quantity;
                data.Count.Value = itemCount;

                data.TitleText.Value = model.FungibleAssetValue.GetLocalizedName();
                data.Submittable.Value = true;
                data.Item.Value = new CountEditableItem(model.FungibleAssetValue,
                    itemCount,
                    itemCount,
                    itemCount);

            }

            data.Item.Value.CountEnabled.Value = false;
        }

        private async void SubscribeReRegisterProduct(bool chargeAp)
        {
            var itemProducts = ReactiveShopState.SellItemProducts.Value;
            var favProducts = ReactiveShopState.SellFungibleAssetProducts.Value;

            if (!itemProducts.Any() && !favProducts.Any())
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_SHOP_NONEUPDATESELLALL"),
                    NotificationCell.NotificationType.Alert);

                return;
            }

            // view.SetLoading(itemProducts);
            var oneLineSystemInfos = new List<(string name, int count)>();
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var reRegisterInfos = new List<(IProductInfo, IRegisterInfo)>();
            await foreach (var product in itemProducts)
            {
                reRegisterInfos.Add(GetReRegisterInfo(product.ProductId, (int)product.Price));
                var (itemName, _, _) = await Game.Game.instance.MarketServiceClient.GetProductInfo(product.ProductId);
                oneLineSystemInfos.Add((itemName, (int)product.Quantity));
            }

            await foreach (var product in favProducts)
            {
                reRegisterInfos.Add(GetReRegisterInfo(product.ProductId, (int)product.Price));
                var (itemName, _, _) = await Game.Game.instance.MarketServiceClient.GetProductInfo(product.ProductId);
                oneLineSystemInfos.Add((itemName, (int)product.Quantity));
            }

            Game.Game.instance.ActionManager.ReRegisterProduct(avatarAddress, reRegisterInfos, chargeAp).Subscribe();

            Analyzer.Instance.Track("Unity/ReRegisterProductAll", new Dictionary<string, Value>()
            {
                ["Quantity"] = reRegisterInfos.Count,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });

            var evt = new AirbridgeEvent("ReRegisterProduct_All");
            evt.SetValue(reRegisterInfos.Count);
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            string message;
            if (reRegisterInfos.Count() > 1)
            {
                message = L10nManager.Localize("NOTIFICATION_REREGISTER_ALL_START");
            }
            else
            {
                var info = oneLineSystemInfos.FirstOrDefault();
                if (info.count > 1)
                {
                    message = string.Format(
                        L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_START"),
                        info.name, info.count);
                }
                else
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_START"),
                        info.name);
                }
            }

            OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Information);
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
        }

        private async void SubscribeCancelProductRegistration(bool chargeAp)
        {
            var itemProducts = ReactiveShopState.SellItemProducts.Value;
            var favProducts = ReactiveShopState.SellFungibleAssetProducts.Value;

            if (!itemProducts.Any() && !favProducts.Any())
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_SHOP_NONCANCELLATIONALL"),
                    NotificationCell.NotificationType.Alert);

                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var goldCurrency = States.Instance.GoldBalanceState.Gold.Currency;

            var oneLineSystemInfos = new List<(string name, int count)>();
            var productInfos = new List<IProductInfo>();
            foreach (var itemProduct in itemProducts)
            {
                productInfos.Add(new ItemProductInfo()
                {
                    ProductId = itemProduct.ProductId,
                    Price = new FungibleAssetValue(goldCurrency,
                        (BigInteger)itemProduct.Price, 0),
                    AgentAddress = itemProduct.SellerAgentAddress,
                    AvatarAddress = itemProduct.SellerAvatarAddress,
                    Type = itemProduct.ItemType == ItemType.Material
                        ? ProductType.Fungible
                        : ProductType.NonFungible,
                    Legacy = itemProduct.Legacy,
                    ItemSubType = itemProduct.ItemSubType,
                    TradableId = itemProduct.TradableId
                });

                var (itemName, _, _) = await Game.Game.instance.MarketServiceClient.GetProductInfo(itemProduct.ProductId);
                oneLineSystemInfos.Add((itemName, (int)itemProduct.Quantity));
            }

            foreach (var favProduct in favProducts)
            {
                productInfos.Add(new FavProductInfo()
                {
                    ProductId = favProduct.ProductId,
                    Price = new FungibleAssetValue(goldCurrency, (BigInteger)favProduct.Price, 0),
                    AgentAddress = favProduct.SellerAgentAddress,
                    AvatarAddress = favProduct.SellerAvatarAddress,
                    Type = ProductType.FungibleAssetValue,
                });

                var (itemName, _, _) = await Game.Game.instance.MarketServiceClient.GetProductInfo(favProduct.ProductId);
                oneLineSystemInfos.Add((itemName, (int)favProduct.Quantity));
            }

            Game.Game.instance.ActionManager.CancelProductRegistration(avatarAddress, productInfos, chargeAp).Subscribe();
            Analyzer.Instance.Track("Unity/CancelRegisterProductAll", new Dictionary<string, Value>()
            {
                ["Quantity"] = productInfos.Count,
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });

            string message;
            if (productInfos.Count > 1)
            {
                message = L10nManager.Localize("NOTIFICATION_CANCELREGISTER_ALL_START");
            }
            else
            {
                var info = oneLineSystemInfos.FirstOrDefault();
                if (info.count > 1)
                {
                    message = string.Format(
                        L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_CANCEL_START"),
                        info.name, info.count);
                }
                else
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_CANCEL_START"),
                        info.name);
                }
            }

            OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Information);
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
        }

        private void ShowRetrievePopup(ShopItem model, bool chargeAp) // 판매 취소
        {
            var productId = model.Product?.ProductId ?? model.FungibleAssetProduct.ProductId;
            var price = model.Product?.Price ?? model.FungibleAssetProduct.Price;
            var quantity = model.Product?.Quantity ?? model.FungibleAssetProduct.Quantity;

            SharedModel.ItemCountAndPricePopup.Value.TitleText.Value =
                L10nManager.Localize("UI_RETRIEVE");
            SharedModel.ItemCountAndPricePopup.Value.InfoText.Value =
                L10nManager.Localize("UI_RETRIEVE_INFO");
            SharedModel.ItemCountAndPricePopup.Value.CountEnabled.Value = true;
            SharedModel.ItemCountAndPricePopup.Value.ProductId.Value = productId;
            SharedModel.ItemCountAndPricePopup.Value.Price.Value = (BigInteger)price *
                States.Instance.GoldBalanceState.Gold.Currency;
            SharedModel.ItemCountAndPricePopup.Value.PriceInteractable.Value = false;
            SharedModel.ItemCountAndPricePopup.Value.ChargeAp.Value = chargeAp;
            var itemCount = (int)quantity;
            if (model.Product is null)
            {
                SharedModel.ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                    model.FungibleAssetValue,
                    itemCount,
                    itemCount,
                    itemCount);
            }
            else
            {
                SharedModel.ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                    model.ItemBase,
                    itemCount,
                    itemCount,
                    itemCount);
            }
        }

        // sell
        private void SubscribeSellPopup(CountableItem data)
        {
            if (data is null)
            {
                Find<ItemCountableAndPricePopup>().Close();
                return;
            }

            Find<ItemCountableAndPricePopup>().Show(SharedModel.ItemCountableAndPricePopup.Value,
                SharedModel.ItemCountableAndPricePopup.Value.IsSell.Value);
        }

        private void SubscribeRegisterProduct(Model.ItemCountableAndPricePopup data)
        {
            if (data.Price.Value.MinorUnit > 0)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_TOTAL_PRICE_WARNING"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (data.Price.Value.Sign * data.Price.Value.MajorUnit < Shop.MinimumPrice)
            {
                throw new InvalidSellingPriceException(data);
            }

            if (data.Item.Value.ItemBase.Value is not null)
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var itemBase = data.Item.Value.ItemBase.Value;
                var type = itemBase.ItemSubType is ItemSubType.Hourglass or ItemSubType.ApStone
                    ? ProductType.Fungible
                    : ProductType.NonFungible;
                var count = data.Count.Value;

                List<ITradableItem> tradableItems;
                FungibleAssetValue price;
                int itemCount;
                if (type == ProductType.NonFungible && count > 1)  // reference: RegisterInfo.Validate()
                {
                    var consumablesInventory =
                        States.Instance.CurrentAvatarState.inventory.Consumables.ToArray();

                    var id = itemBase.Id;
                    // If the item is consumable, it need to sell the same item multiple times.
                    var consumablesToSell = new List<Consumable>();
                    for (int i = 0; i < count; i++)
                    {
                        var item = consumablesInventory.FirstOrDefault(consumable =>
                            consumable.Id == id && !consumablesToSell.Contains(consumable));
                        if (item != null)
                        {
                            consumablesToSell.Add(item);
                        }
                    }

                    tradableItems = consumablesToSell.Cast<ITradableItem>().ToList();
                    price = data.UnitPrice.Value;
                    itemCount = 1;
                }
                else
                {
                    if (itemBase is not ITradableItem tradableItem)
                    {
                        return;
                    }

                    tradableItems = new List<ITradableItem> { tradableItem };
                    price = data.Price.Value;
                    itemCount = count;
                }

                var infos = tradableItems
                    .Select(tradableItem => new RegisterInfo
                    {
                        AvatarAddress = avatarAddress,
                        Price = price,
                        TradableId = tradableItem.TradableId,
                        ItemCount = itemCount,
                        Type = type,
                    }).Cast<IRegisterInfo>().ToList();

                Game.Game.instance.ActionManager
                    .RegisterProduct(avatarAddress, infos, data.ChargeAp.Value).Subscribe();

                foreach (var tradableItem in tradableItems)
                {
                    if (tradableItem is not TradableMaterial)
                    {
                        LocalLayerModifier.RemoveItem(avatarAddress, tradableItem.TradableId,
                            tradableItem.RequiredBlockIndex,
                            itemCount);
                    }

                    LocalLayerModifier.SetItemEquip(avatarAddress, tradableItem.TradableId, false);
                }

                PostRegisterProduct(itemBase.GetLocalizedName());
            }
            else
            {
                var count = data.Count.Value;
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var currency = data.Item.Value.FungibleAssetValue.Value.Currency;
                var fungibleAsset = new FungibleAssetValue(currency, count, 0);
                var info = new AssetInfo
                {
                    AvatarAddress = avatarAddress,
                    Price = data.Price.Value,
                    Asset = fungibleAsset,
                    Type = ProductType.FungibleAssetValue
                };
                var infos = new List<IRegisterInfo> { info };

                Game.Game.instance.ActionManager
                    .RegisterProduct(avatarAddress, infos, data.ChargeAp.Value).Subscribe();
                var preBalance = States.Instance.CurrentAvatarBalances[currency.Ticker];
                States.Instance.SetCurrentAvatarBalance(preBalance - fungibleAsset);
                inventory.UpdateFungibleAssets();
                PostRegisterProduct(fungibleAsset.GetLocalizedName());
            }
        }

        private void SubscribeReRegisterProduct(Model.ItemCountableAndPricePopup data)
        {
            if (data.Price.Value.MinorUnit > 0)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_TOTAL_PRICE_WARNING"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (data.Price.Value.Sign * data.Price.Value.MajorUnit < Shop.MinimumPrice)
            {
                throw new InvalidSellingPriceException(data);
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var reRegisterInfos = new List<(IProductInfo, IRegisterInfo)>
            {
                GetReRegisterInfo(data.ProductId.Value, (int)data.Price.Value.MajorUnit)
            };

            var itemName = data.Item.Value.ItemBase.Value is not null
                ? data.Item.Value.ItemBase.Value.GetLocalizedName()
                : data.Item.Value.FungibleAssetValue.Value.GetLocalizedName();
            PostRegisterProduct(itemName);

            Game.Game.instance.ActionManager
                .ReRegisterProduct(avatarAddress, reRegisterInfos, data.ChargeAp.Value).Subscribe();

            Analyzer.Instance.Track("Unity/ReRegisterProduct", new Dictionary<string, Value>()
            {
                ["AvatarAddress"] = avatarAddress.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            }, true);

            var evt = new AirbridgeEvent("ReRegisterProduct");
            evt.AddCustomAttribute("agent-address", States.Instance.CurrentAvatarState.address.ToString());
            evt.AddCustomAttribute("avatar-address", States.Instance.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);
        }

        private static (IProductInfo, IRegisterInfo) GetReRegisterInfo(Guid productId, int newPrice)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var goldCurrency = States.Instance.GoldBalanceState.Gold.Currency;
            var itemProduct = ReactiveShopState.GetSellItemProduct(productId);
            if (itemProduct is not null)
            {
                var productInfo = new ItemProductInfo()
                {
                    ProductId = itemProduct.ProductId,
                    Price = new FungibleAssetValue(goldCurrency, (BigInteger)itemProduct.Price, 0),
                    AgentAddress = itemProduct.SellerAgentAddress,
                    AvatarAddress = itemProduct.SellerAvatarAddress,
                    Type = itemProduct.ItemType == ItemType.Material
                        ? ProductType.Fungible
                        : ProductType.NonFungible,
                    Legacy = itemProduct.Legacy,
                    ItemSubType = itemProduct.ItemSubType,
                    TradableId = itemProduct.TradableId
                };

                var registerInfo = new RegisterInfo
                {
                    AvatarAddress = avatarAddress,
                    Price = new FungibleAssetValue(goldCurrency, (BigInteger)newPrice, 0),
                    TradableId = itemProduct.TradableId,
                    ItemCount = (int)itemProduct.Quantity,
                    Type = itemProduct.ItemSubType is ItemSubType.Hourglass or ItemSubType.ApStone
                        ? ProductType.Fungible
                        : ProductType.NonFungible
                };

                return (productInfo, registerInfo);
            }

            var favProduct = ReactiveShopState.GetSellFungibleAssetProduct(productId);
            if (favProduct is not null)
            {
                var productInfo = new FavProductInfo()
                {
                    ProductId = favProduct.ProductId,
                    Price = new FungibleAssetValue(goldCurrency, (BigInteger)favProduct.Price, 0),
                    AgentAddress = favProduct.SellerAgentAddress,
                    AvatarAddress = favProduct.SellerAvatarAddress,
                    Type = ProductType.FungibleAssetValue,
                };

                var currency = Currency.Legacy(favProduct.Ticker, 0, null);
                var fungibleAsset = new FungibleAssetValue(currency, (BigInteger)favProduct.Quantity, 0);
                var registerInfo = new AssetInfo
                {
                    AvatarAddress = avatarAddress,
                    Price = new FungibleAssetValue(goldCurrency, (BigInteger)newPrice, 0),
                    Asset = fungibleAsset,
                    Type = ProductType.FungibleAssetValue
                };

                return (productInfo, registerInfo);
            }

            return (null, null);
        }

        private void SubscribeSellPopupCancel(Model.ItemCountableAndPricePopup data)
        {
            SharedModel.ItemCountableAndPricePopup.Value.Item.Value = null;
            Find<ItemCountableAndPricePopup>().Close();
        }

        private void SubscribeSellPopupCount(int count)
        {
            SharedModel.ItemCountableAndPricePopup.Value.Count.Value = count;
            UpdateUnitPrice();
        }

        private void SubscribeSellPopupPrice(decimal price)
        {
            var model = SharedModel.ItemCountableAndPricePopup.Value;

            if (price > LimitPrice)
            {
                price = LimitPrice;

                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_SELL_LIMIT_EXCEEDED"),
                    NotificationCell.NotificationType.Alert);
                NcDebug.LogError(L10nManager.Localize("UI_SELL_LIMIT_EXCEEDED"));
            }

            var currency = model.Price.Value.Currency;
            var major = (int)price;
            var minor = (int)((Math.Truncate((price - major) * 100) / 100) * 100);

            var fungibleAsset = new FungibleAssetValue(currency, major, minor);
            model.Price.SetValueAndForceNotify(fungibleAsset);
            UpdateUnitPrice();
        }

        private void UpdateUnitPrice()
        {
            var model = SharedModel.ItemCountableAndPricePopup.Value;

            decimal price = 0;
            if (decimal.TryParse(model.Price.Value.GetQuantityString(),
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var result))
            {
                price = result;
            }

            var count = model.Count.Value;
            var unitPrice = price / count;

            var currency = model.UnitPrice.Value.Currency;
            var major = (int)unitPrice;
            var minor = (int)((Math.Truncate((unitPrice - major) * 100) / 100) * 100);

            var fungibleAsset = new FungibleAssetValue(currency, major, minor);
            model.UnitPrice.SetValueAndForceNotify(fungibleAsset);
        }

        // sell cancellation
        private void SubscribeSellCancellationPopup(CountableItem data)
        {
            if (data is null)
            {
                Find<TwoButtonSystem>().Close();
                return;
            }

            Find<TwoButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCELLATION"),
                L10nManager.Localize("UI_YES"),
                L10nManager.Localize("UI_NO"),
                SubscribeSellCancellationPopupSubmit,
                SubscribeSellCancellationPopupCancel);
        }

        private void SubscribeSellCancellationPopupSubmit()
        {
            var model = SharedModel.ItemCountAndPricePopup.Value;
            var itemProduct = ReactiveShopState.GetSellItemProduct(model.ProductId.Value);
            if (itemProduct is not null)
            {
                var productInfo = new List<IProductInfo>
                {
                    new ItemProductInfo()
                    {
                        ProductId = itemProduct.ProductId,
                        Price = new FungibleAssetValue(model.Price.Value.Currency,
                            (BigInteger)itemProduct.Price, 0),
                        AgentAddress = itemProduct.SellerAgentAddress,
                        AvatarAddress = itemProduct.SellerAvatarAddress,
                        Type = itemProduct.ItemType == ItemType.Material
                            ? ProductType.Fungible
                            : ProductType.NonFungible,
                        Legacy = itemProduct.Legacy,
                        ItemSubType = itemProduct.ItemSubType,
                        TradableId = itemProduct.TradableId
                    }
                };
                Game.Game.instance.ActionManager.CancelProductRegistration(
                    itemProduct.SellerAvatarAddress, productInfo, model.ChargeAp.Value).Subscribe();
            }
            else
            {
                var fav = ReactiveShopState.GetSellFungibleAssetProduct(model.ProductId.Value);
                var productInfo = new List<IProductInfo>()
                {
                    new FavProductInfo()
                    {
                        ProductId = fav.ProductId,
                        Price = new FungibleAssetValue(model.Price.Value.Currency,
                            (BigInteger)fav.Price, 0),
                        AgentAddress = fav.SellerAgentAddress,
                        AvatarAddress = fav.SellerAvatarAddress,
                        Type = ProductType.FungibleAssetValue,
                    }
                };
                Game.Game.instance.ActionManager.CancelProductRegistration(
                    fav.SellerAvatarAddress, productInfo, model.ChargeAp.Value).Subscribe();
            }

            ResponseCancelProductRegistration();
        }

        private void SubscribeSellCancellationPopupCancel()
        {
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
            Find<TwoButtonSystem>().Close();
        }

        private static bool DimmedFuncForSell(ItemBase itemBase)
        {
            if (itemBase.ItemType == ItemType.Material)
            {
                return !(itemBase is TradableMaterial);
            }

            return false;
        }

        private void PostRegisterProduct(string itemName)
        {
            var count = SharedModel.ItemCountableAndPricePopup.Value.Count.Value;
            SharedModel.ItemCountableAndPricePopup.Value.Item.Value = null;
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);

            var message = string.Empty;
            if (count > 1)
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_START"),
                    itemName,
                    count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_START"),
                    itemName);
            }

            OneLineSystem.Push(MailType.Auction, message,
                NotificationCell.NotificationType.Information);
        }

        private void ResponseCancelProductRegistration()
        {
            var count = SharedModel.ItemCountAndPricePopup.Value.Item.Value.Count.Value;
            var item = SharedModel.ItemCountAndPricePopup.Value.Item.Value;
            var itemName = item.ItemBase.Value is not null
                ? item.ItemBase.Value.GetLocalizedName()
                : item.FungibleAssetValue.Value.GetLocalizedName();

            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);

            string message;
            if (count > 1)
            {
                message = string.Format(
                    L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_CANCEL_START"),
                    itemName, count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_CANCEL_START"),
                    itemName);
            }

            OneLineSystem.Push(MailType.Auction, message,
                NotificationCell.NotificationType.Information);

            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
        }
    }
}
