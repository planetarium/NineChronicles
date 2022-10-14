using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
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
        private enum PriorityType
        {
            Price,
            Count,
        }

        [SerializeField]
        private Inventory inventory;

        [SerializeField]
        private SellView view;

        [SerializeField]
        private SpeechBubble speechBubble = null;

        [SerializeField]
        private Button reregistrationButton;

        [SerializeField]
        private Button buyButton = null;

        [SerializeField]
        private Button closeButton = null;

        private const int LimitPrice = 100000000;

        private Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SharedModel = new Shop();
            CloseWidget = null;

            reregistrationButton.onClick.AddListener(() =>
            {
                Find<TwoButtonSystem>().Show(
                    L10nManager.Localize("UI_SHOP_UPDATESELLALL_POPUP"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    SubscribeUpdateSellPopupSubmit);
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

            SharedModel.ItemCountableAndPricePopup.Value.Item
                .Subscribe(SubscribeSellPopup)
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnClickSubmit
                .Subscribe(SubscribeSellPopupSubmit)
                .AddTo(gameObject);
            SharedModel.ItemCountableAndPricePopup.Value.OnClickReregister
                .Subscribe(SubscribeSellPopupUpdateSell)
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

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            tooltip.Show(
                model,
                L10nManager.Localize("UI_SELL"),
                model.ItemBase is ITradableItem,
                () => ShowSell(model),
                inventory.ClearSelectedItem,
                () => L10nManager.Localize("UI_UNTRADABLE"),
                target);
        }

        private void ShowSellTooltip(ShopItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            tooltip.Show(model,
                () => ShowUpdateSellPopup(model),
                () => ShowRetrievePopup(model),
                view.ClearSelectedItem, target);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            UpdateSpeechBubble();
            inventory.SetShop(ShowItemTooltip);

            var task = Task.Run(async () =>
            {
                await ReactiveShopState.UpdateSellDigestsAsync();
                return true;
            });

            var result = await task;
            if (result)
            {
                view.Show(ReactiveShopState.SellDigest, ShowSellTooltip);
            }
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

            data.TitleText.Value = model.ItemBase.GetLocalizedName();
            data.InfoText.Value = string.Empty;
            data.CountEnabled.Value = true;
            data.Submittable.Value = !DimmedFuncForSell(model.ItemBase);
            data.Item.Value = new CountEditableItem(model.ItemBase,
                1,
                1,
                model.Count.Value);
            data.Item.Value.CountEnabled.Value = false;
        }

        private void ShowUpdateSellPopup(ShopItem model)
        {
            var data = SharedModel.ItemCountableAndPricePopup.Value;

            if (decimal.TryParse(model.OrderDigest.Price.GetQuantityString(),
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var price))
            {
                var unitPrice = price / model.OrderDigest.ItemCount;
                var majorUnit = (int)unitPrice;
                var minorUnit = (int)((unitPrice - majorUnit) * 100);
                var currency = States.Instance.GoldBalanceState.Gold.Currency;
                data.UnitPrice.Value = new FungibleAssetValue(currency, majorUnit, minorUnit);
            }

            data.PrePrice.Value = model.OrderDigest.Price;
            data.Price.Value = model.OrderDigest.Price;
            data.Count.Value = model.OrderDigest.ItemCount;
            data.IsSell.Value = false;

            data.TitleText.Value = model.ItemBase.GetLocalizedName();
            data.InfoText.Value = string.Empty;
            data.CountEnabled.Value = true;
            data.Submittable.Value = !DimmedFuncForSell(model.ItemBase);
            data.Item.Value = new CountEditableItem(model.ItemBase,
                model.OrderDigest.ItemCount,
                model.OrderDigest.ItemCount,
                model.OrderDigest.ItemCount);
            data.Item.Value.CountEnabled.Value = false;
        }

        private void SubscribeUpdateSellPopupSubmit()
        {
            var digests = ReactiveShopState.SellDigest.Value;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var orderDigests = digests.Where(d => d.ExpiredBlockIndex - blockIndex <= 0).ToList();

            if (!orderDigests.Any())
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_SHOP_NONEUPDATESELLALL"),
                    NotificationCell.NotificationType.Alert);

                return;
            }
            view.SetLoading(orderDigests);

            var updateSellInfos = new List<UpdateSellInfo>();
            var oneLineSystemInfos = new List<(string name, int count)>();
            foreach (var orderDigest in orderDigests)
            {
                if (!ReactiveShopState.TryGetShopItem(orderDigest, out var itemBase))
                {
                    return;
                }

                var updateSellInfo = new UpdateSellInfo(
                    orderDigest.OrderId,
                    Guid.NewGuid(),
                    orderDigest.TradableId,
                    itemBase.ItemSubType,
                    orderDigest.Price,
                    orderDigest.ItemCount
                );

                updateSellInfos.Add(updateSellInfo);
                oneLineSystemInfos.Add((itemBase.GetLocalizedName(), orderDigest.ItemCount));
            }

            Game.Game.instance.ActionManager.UpdateSell(updateSellInfos).Subscribe();
            Tracer.Instance.Trace("Unity/UpdateSellAll", new Dictionary<string, string>()
            {
                ["Quantity"] = updateSellInfos.Count.ToString(),
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });

            string message;
            if (updateSellInfos.Count() > 1)
            {
                message = L10nManager.Localize("NOTIFICATION_REREGISTER_ALL_START");
            }
            else
            {
                var info = oneLineSystemInfos.FirstOrDefault();
                if (info.count > 1)
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_START"),
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

        private void ShowRetrievePopup(ShopItem model)
        {
            SharedModel.ItemCountAndPricePopup.Value.TitleText.Value =
                L10nManager.Localize("UI_RETRIEVE");
            SharedModel.ItemCountAndPricePopup.Value.InfoText.Value =
                L10nManager.Localize("UI_RETRIEVE_INFO");
            SharedModel.ItemCountAndPricePopup.Value.CountEnabled.Value = true;
            SharedModel.ItemCountAndPricePopup.Value.Price.Value = model.OrderDigest.Price;
            SharedModel.ItemCountAndPricePopup.Value.PriceInteractable.Value = false;
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                model.ItemBase,
                model.OrderDigest.ItemCount,
                model.OrderDigest.ItemCount,
                model.OrderDigest.ItemCount);
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

        private void SubscribeSellPopupSubmit(Model.ItemCountableAndPricePopup data)
        {
            if (!(data.Item.Value.ItemBase.Value is ITradableItem tradableItem))
            {
                return;
            }

            if (data.Price.Value.MinorUnit > 0)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_TOTAL_PRICE_WARNING"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (data.Price.Value.Sign * data.Price.Value.MajorUnit <
                Model.Shop.MinimumPrice)
            {
                throw new InvalidSellingPriceException(data);
            }

            var totalPrice = data.Price.Value;
            var count = data.Count.Value;
            var itemSubType = data.Item.Value.ItemBase.Value.ItemSubType;
            Game.Game.instance.ActionManager.Sell(tradableItem, count, totalPrice, itemSubType)
                .Subscribe();
            Tracer.Instance.Trace("Unity/Sell", new Dictionary<string, string>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });
            ResponseSell();
        }

        private void SubscribeSellPopupUpdateSell(Model.ItemCountableAndPricePopup data)
        {
            if (!(data.Item.Value.ItemBase.Value is ITradableItem tradableItem))
            {
                return;
            }

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

            var requiredBlockIndex = tradableItem.RequiredBlockIndex;
            var totalPrice = data.Price.Value;
            var preTotalPrice = data.PrePrice.Value;
            var count = data.Count.Value;
            var digest =
                ReactiveShopState.GetSellDigest(tradableItem.TradableId, requiredBlockIndex,
                    preTotalPrice, count);
            if (digest == null)
            {
                return;
            }

            var itemSubType = data.Item.Value.ItemBase.Value.ItemSubType;
            var updateSellInfo = new UpdateSellInfo(
                digest.OrderId,
                Guid.NewGuid(),
                tradableItem.TradableId,
                itemSubType,
                totalPrice,
                count
            );

            Game.Game.instance.ActionManager.UpdateSell(new List<UpdateSellInfo> {updateSellInfo}).Subscribe();
            Tracer.Instance.Trace("Unity/UpdateSell", new Dictionary<string, string>()
            {
                ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
            });
            ResponseSell();
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
                Debug.LogError(L10nManager.Localize("UI_SELL_LIMIT_EXCEEDED"));
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
            if (!(model.Item.Value.ItemBase.Value is ITradableItem tradableItem))
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var tradableId = tradableItem.TradableId;
            var requiredBlockIndex = tradableItem.RequiredBlockIndex;
            var price = model.Price.Value;
            var count = model.Item.Value.Count.Value;
            var subType = tradableItem.ItemSubType;

            var digest =
                ReactiveShopState.GetSellDigest(tradableId, requiredBlockIndex, price, count);
            if (digest != null)
            {
                Tracer.Instance.Trace("Unity/Sell Cancellation", new Dictionary<string, string>()
                {
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                });
                Game.Game.instance.ActionManager.SellCancellation(
                    avatarAddress,
                    digest.OrderId,
                    digest.TradableId,
                    subType).Subscribe();
                ResponseSellCancellation(digest.OrderId, digest.TradableId);
            }
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

        private void ResponseSell()
        {
            var item = SharedModel.ItemCountableAndPricePopup.Value.Item.Value;
            var count = SharedModel.ItemCountableAndPricePopup.Value.Count.Value;
            SharedModel.ItemCountableAndPricePopup.Value.Item.Value = null;

            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);

            string message = string.Empty;
            if (count > 1)
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_START"),
                    item.ItemBase.Value.GetLocalizedName(),
                    count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_START"),
                    item.ItemBase.Value.GetLocalizedName());
            }

            OneLineSystem.Push(MailType.Auction, message,
                NotificationCell.NotificationType.Information);
        }

        private async void ResponseSellCancellation(Guid orderId, Guid tradableId)
        {
            var count = SharedModel.ItemCountAndPricePopup.Value.Item.Value.Count.Value;
            SharedModel.ItemCountAndPricePopup.Value.Item.Value = null;
            var itemName = await Util.GetItemNameByOrderId(orderId);
            ReactiveShopState.RemoveSellDigest(orderId);
            AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);

            string message;
            if (count > 1)
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_CANCEL_START"),
                    itemName, count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_CANCEL_START"), itemName);
            }

            OneLineSystem.Push(MailType.Auction, message, NotificationCell.NotificationType.Information);
        }
    }
}
