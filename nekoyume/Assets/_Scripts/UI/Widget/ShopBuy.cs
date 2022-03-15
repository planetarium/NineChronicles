using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Assets;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
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
        private Button sellButton = null;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private BuyView view;

        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

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
                })
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            Find<DataLoadingScreen>().Show();
            Game.Game.instance.Stage.GetPlayer().gameObject.SetActive(false);

            var initWeaponTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType>() { ItemSubType.Weapon, };
                await ReactiveShopState.SetBuyDigests(list);
                return true;
            });

            var initWeaponResult = await initWeaponTask;
            if (initWeaponResult)
            {
                base.Show(ignoreShowAnimation);
                view.Show(ReactiveShopState.BuyDigest, ShowItemTooltip);
                Find<DataLoadingScreen>().Close();
                HelpTooltip.HelpMe(100018, true);
                AudioController.instance.PlayMusic(AudioController.MusicCode.Shop);
            }

            var initOthersTask = Task.Run(async () =>
            {
                var list = new List<ItemSubType>()
                {
                    ItemSubType.Armor,
                    ItemSubType.Belt,
                    ItemSubType.Necklace,
                    ItemSubType.Ring,
                    ItemSubType.Food,
                    ItemSubType.FullCostume,
                    ItemSubType.HairCostume,
                    ItemSubType.EarCostume,
                    ItemSubType.EyeCostume,
                    ItemSubType.TailCostume,
                    ItemSubType.Title,
                    ItemSubType.Hourglass,
                    ItemSubType.ApStone,
                };
                await ReactiveShopState.SetBuyDigests(list);
                return true;
            }, _cancellationTokenSource.Token);

            if (initOthersTask.IsCanceled)
            {
                return;
            }

            var initOthersResult = await initOthersTask;
            if (!initOthersResult)
            {
                return;
            }

            view.IsDoneLoadItem = true;
        }

        public void Open()
        {
            base.Show(true);
            view.Show(ReactiveShopState.BuyDigest, ShowItemTooltip);
        }

        public override void Close(bool ignoreCloseAnimation = false)
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

        private void ShowItemTooltip(ShopItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            tooltip.Show(target, model,
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
                sumPrice += model.OrderDigest.Price;
            }

            if (States.Instance.GoldBalanceState.Gold < sumPrice)
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                    NotificationCell.NotificationType.Information);
                return;
            }

            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"),
                models.Count(), sumPrice);

            Find<TwoButtonSystem>().Show(content, L10nManager.Localize("UI_BUY"),
                L10nManager.Localize("UI_CANCEL"),
                (() => Buy(models)));
        }

        private async void Buy(List<ShopItem> models)
        {
            var purchaseInfos = new ConcurrentBag<PurchaseInfo>();
            await foreach (var item in models.ToAsyncEnumerable())
            {
                var purchaseInfo = await GetPurchaseInfo(item.OrderDigest.OrderId);
                purchaseInfos.Add(purchaseInfo);
            }

            Game.Game.instance.ActionManager.Buy(purchaseInfos.ToList()).Subscribe();

            if (models.Count > 0)
            {
                var props = new Value
                {
                    ["Count"] = models.Count,
                };
                Analyzer.Instance.Track("Unity/Number of Purchased Items", props);
            }

            foreach (var model in models)
            {
                var props = new Value
                {
                    ["Price"] = model.OrderDigest.Price.GetQuantityString(),
                };
                Analyzer.Instance.Track("Unity/Buy", props);

                var count = model.OrderDigest.ItemCount;
                model.Selected.Value = false;
                ReactiveShopState.RemoveBuyDigest(model.OrderDigest.OrderId);

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
    }
}
