using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Assets;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    public class ShopBuy : Widget
    {
        [SerializeField]
        private Button sellButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private BuyView view;

        private CancellationTokenSource _cancellationTokenSource = new();

        private Shop SharedModel { get; set; }

        protected override void Awake()
        {
            base.Awake();
            SharedModel = new Shop();

            sellButton.onClick.AddListener(() =>
            {
                Find<ItemCountAndPricePopup>().Close();
                Find<ShopSell>().Show();
                gameObject.SetActive(false);
            });

            closeButton.onClick.AddListener(() => Close());
            CloseWidget = () =>
            {
                if (view.IsFocused)
                {
                    return;
                }

                Close();
            };

            view.SetAction(ShowBuyPopup);
        }

        public override void Initialize()
        {
            base.Initialize();
            SharedModel.ItemCountAndPricePopup.Value.Item.Subscribe(data =>
            {
                if (data is null)
                {
                    Find<ItemCountAndPricePopup>().Close();
                    return;
                }

                Find<ItemCountAndPricePopup>().Pop(SharedModel.ItemCountAndPricePopup.Value);
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            Find<DataLoadingScreen>().Show();
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);
            await ReactiveShopState.RequestBuyProductsAsync(ItemSubType.Weapon, MarketOrderType.cp_desc, 0, 60);
            base.Show(ignoreShowAnimation);
            view.Show(ReactiveShopState.BuyProducts, ShowItemTooltip);
            Find<DataLoadingScreen>().Close();
            HelpTooltip.HelpMe(100018, true);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
        }

        public void Open()
        {
            base.Show(true);
            view.Show(ReactiveShopState.BuyProducts, ShowItemTooltip);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            ReactiveShopState.ClearCache();
            if (view.IsCartEmpty)
            {
                OnClose();
            }
            else
            {
                Find<TwoButtonSystem>().Show(
                    L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                    L10nManager.Localize("UI_YES"),
                    L10nManager.Localize("UI_NO"),
                    OnClose);
            }
        }

        private void OnClose()
        {
            Find<ItemCountAndPricePopup>().Close();
            Game.Event.OnRoomEnter.Invoke(true);
            _cancellationTokenSource.Cancel();
            base.Close(true);
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

        private void ShowItemTooltip(ShopItem model)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            tooltip.Show(model,
                () => ShowBuyPopup(new List<ShopItem> { model }),
                view.ClearSelectedItems);
        }

        private void ShowBuyPopup(List<ShopItem> models)
        {
            if (!models.Any())
            {
                return;
            }

            var sumPrice =
                new FungibleAssetValue(States.Instance.GoldBalanceState.Gold.Currency, 0, 0);
            foreach (var model in models)
            {
                sumPrice += (BigInteger)model.Product.Price * States.Instance.GoldBalanceState.Gold.Currency;
            }

            if (States.Instance.GoldBalanceState.Gold < sumPrice)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"),
                models.Count, sumPrice);
            Find<TwoButtonSystem>().Show(
                content,
                L10nManager.Localize("UI_BUY"),
                L10nManager.Localize("UI_CANCEL"),
                (() => Buy(models)));
        }

        private async void Buy(List<ShopItem> models)
        {
            var purchaseInfos = new ConcurrentBag<PurchaseInfo>();
            await foreach (var item in models.ToAsyncEnumerable())
            {
                var purchaseInfo = await GetPurchaseInfo(item.Product.ProductId);
                purchaseInfos.Add(purchaseInfo);
            }

            Game.Game.instance.ActionManager.Buy(purchaseInfos.ToList()).Subscribe();

            if (models.Count > 0)
            {
                var props = new Dictionary<string, Value>()
                {
                    ["Count"] = models.Count,
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                };
                Analyzer.Instance.Track("Unity/Number of Purchased Items", props);
            }

            foreach (var model in models)
            {
                var props = new Dictionary<string, Value>()
                {
                    ["Price"] = model.Product.Price,
                    ["AvatarAddress"] = States.Instance.CurrentAvatarState.address.ToString(),
                    ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                };
                Analyzer.Instance.Track("Unity/Buy", props);

                var count = model.Product.Quantity;
                model.Selected.Value = false;
                ReactiveShopState.RemoveBuyProduct(model.Product.ProductId);

                string message;
                if (count > 1)
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_START"),
                        model.ItemBase.GetLocalizedName(), count);
                }
                else
                {
                    message = string.Format(L10nManager.Localize("NOTIFICATION_BUY_START"),
                        model.ItemBase.GetLocalizedName());
                }

                OneLineSystem.Push(MailType.Auction, message,
                    NotificationCell.NotificationType.Information);
            }

            view.ClearSelectedItems();
            AudioController.instance.PlaySfx(AudioController.SfxCode.BuyItem);
        }

        private static async Task<PurchaseInfo> GetPurchaseInfo(System.Guid orderId)
        {
            var order = await Util.GetOrder(orderId);
            return new PurchaseInfo(orderId, order.TradableId, order.SellerAgentAddress,
                order.SellerAvatarAddress, order.ItemSubType, order.Price);
        }

        private static ProductInfo GetProductInfo(ShopItem shopItem)
        {
            return new ProductInfo
            {
                AvatarAddress = shopItem.Product.SellerAvatarAddress,
                AgentAddress = shopItem.Product.SellerAgentAddress,
                ProductId = shopItem.Product.ProductId,
                Price = (BigInteger) shopItem.Product.Price *
                        States.Instance.GoldBalanceState.Gold.Currency,
                Type = shopItem.ItemBase is TradableMaterial
                    ? ProductType.Fungible
                    : ProductType.NonFungible,
                Legacy = shopItem.Product.Legacy,
            };
        }
    }
}
