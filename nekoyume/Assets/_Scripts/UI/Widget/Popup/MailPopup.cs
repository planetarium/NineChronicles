using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class MailPopup : PopupWidget, IMail
    {
        public enum MailTabState : int
        {
            All,
            Workshop,
            Market,
            System
        }

        [SerializeField]
        private MailTabState tabState = default;

        [SerializeField]
        private CategoryTabButton allButton = null;

        [SerializeField]
        private CategoryTabButton workshopButton = null;

        [SerializeField]
        private CategoryTabButton marketButton = null;

        [SerializeField]
        private CategoryTabButton systemButton = null;

        [SerializeField]
        private Button receiveAllButton;

        [SerializeField]
        private GameObject receiveAllContainer;

        [SerializeField]
        private MailScroll scroll = null;

        [SerializeField]
        private GameObject emptyImage = null;

        [SerializeField]
        private TextMeshProUGUI emptyText = null;

        [SerializeField]
        private string emptyTextL10nKey = null;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private GameObject loading;

        private readonly Module.ToggleGroup _toggleGroup = new Module.ToggleGroup();

        private const int TutorialEquipmentId = 10110000;

        public MailBox MailBox { get; private set; }

        #region override

        protected override void Awake()
        {
            base.Awake();
            _toggleGroup.RegisterToggleable(allButton);
            _toggleGroup.RegisterToggleable(workshopButton);
            _toggleGroup.RegisterToggleable(marketButton);
            _toggleGroup.RegisterToggleable(systemButton);
            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });

            receiveAllButton.onClick.AddListener(ReceiveAll);
        }

        private async void ReceiveAll()
        {
            var mailRewards = new List<MailReward>();
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

            loading.SetActive(true);
            foreach (var mail in MailBox)
            {
                if (mail.New && mail.requiredBlockIndex <= currentBlockIndex)
                {
                    await AddRewards(mail, mailRewards);
                }
            }

            foreach (var mail in MailBox)
            {
                if (!mail.New || mail.requiredBlockIndex > currentBlockIndex)
                {
                    continue;
                }

                switch (mail)
                {
                    case OrderBuyerMail:
                    case OrderSellerMail:
                    case OrderExpirationMail:
                    case CancelOrderMail:
                    case ProductBuyerMail:
                    case ProductSellerMail:
                    case ProductCancelMail:
                    case UnloadFromMyGaragesRecipientMail:
                        LocalLayerModifier.RemoveNewMail(avatarAddress, mail.id, true);
                        break;
                    case ItemEnhanceMail:
                    case CombinationMail:
                        LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id, true);
                        break;
                }
            }

            loading.SetActive(false);
            ChangeState(0);
            UpdateTabs();
            Find<MailRewardScreen>().Show(mailRewards);
        }

        private static async Task AddRewards(Mail mail, List<MailReward> mailRewards)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            switch (mail)
            {
                case ProductBuyerMail productBuyerMail:
                    var productId = productBuyerMail.ProductId;
                    var (_, itemProduct, favProduct) = await Game.Game.instance.MarketServiceClient.GetProductInfo(productId);
                    if (itemProduct is not null)
                    {
                        var item = States.Instance.CurrentAvatarState.inventory.Items
                            .FirstOrDefault(i => i.item is ITradableItem item &&
                                                 item.TradableId.Equals(itemProduct.TradableId));
                        if (item?.item is null)
                        {
                            return;
                        }

                        mailRewards.Add(new MailReward(item.item, (int)itemProduct.Quantity, true));
                    }

                    if (favProduct is not null)
                    {
                        var currency = Currency.Legacy(favProduct.Ticker, 0, null);
                        var fav = new FungibleAssetValue(currency, (int)favProduct.Quantity, 0);
                        mailRewards.Add(new MailReward(fav, (int)favProduct.Quantity, true));
                    }
                    break;

                case ProductCancelMail productCancelMail:
                    var (_, cItemProduct, cFavProduct) = await Game.Game.instance.MarketServiceClient.GetProductInfo(productCancelMail.ProductId);
                    if (cItemProduct is not null)
                    {
                        var item = States.Instance.CurrentAvatarState.inventory.Items
                            .FirstOrDefault(i => i.item is ITradableItem item &&
                                                 item.TradableId.Equals(cItemProduct.TradableId));
                        if (item?.item is null)
                        {
                            return;
                        }

                        mailRewards.Add(new MailReward(item.item, (int)cItemProduct.Quantity, true));
                    }

                    if (cFavProduct is not null)
                    {
                        var currency = Currency.Legacy(cFavProduct.Ticker, 0, null);
                        var fav = new FungibleAssetValue(currency, (int)cFavProduct.Quantity, 0);
                        mailRewards.Add(new MailReward(fav, (int)cFavProduct.Quantity, true));
                    }
                    break;


                case OrderBuyerMail buyerMail:
                    var bOrder = await Util.GetOrder(buyerMail.OrderId);
                    var bItem = await Util.GetItemBaseByTradableId(bOrder.TradableId, bOrder.ExpiredBlockIndex);
                    var count = bOrder is FungibleOrder bFungibleOrder ? bFungibleOrder.ItemCount : 1;
                    mailRewards.Add(new MailReward(bItem, count, true));
                    break;

                case OrderSellerMail sellerMail:
                    var sOrder = await Util.GetOrder(sellerMail.OrderId);
                    var sItem = await Util.GetItemBaseByTradableId(sOrder.TradableId, sOrder.ExpiredBlockIndex);
                    var sCount = sOrder is FungibleOrder sFungibleOrder ? sFungibleOrder.ItemCount : 1;
                    mailRewards.Add(new MailReward(sItem, sCount));
                    break;

                case OrderExpirationMail expirationMail:
                    var exOrder = await Util.GetOrder(expirationMail.OrderId);
                    var exItem = await Util.GetItemBaseByTradableId(exOrder.TradableId, exOrder.ExpiredBlockIndex);
                    var exCount = exOrder is FungibleOrder exFungibleOrder ? exFungibleOrder.ItemCount : 1;
                    mailRewards.Add(new MailReward(exItem, exCount));
                    break;

                case CancelOrderMail cancelOrderMail:
                    var ccOrder = await Util.GetOrder(cancelOrderMail.OrderId);
                    var ccItem = await Util.GetItemBaseByTradableId(ccOrder.TradableId, ccOrder.ExpiredBlockIndex);
                    var ccCount = ccOrder is FungibleOrder ccFungibleOrder ? ccFungibleOrder.ItemCount : 1;
                    mailRewards.Add(new MailReward(ccItem, ccCount));
                    break;

                case CombinationMail combinationMail:
                    var cItem = combinationMail?.attachment?.itemUsable;
                    if (cItem is not null)
                    {
                        mailRewards.Add(new MailReward(cItem, 1));
                        LocalLayerModifier.AddItem(
                            avatarAddress,
                            cItem.ItemId,
                            cItem.RequiredBlockIndex,
                            1,
                            false);
                    }
                    break;

                case ItemEnhanceMail itemEnhanceMail:
                    var eItem = itemEnhanceMail?.attachment?.itemUsable;
                    if (eItem is not null)
                    {
                        mailRewards.Add(new MailReward(eItem, 1));
                        LocalLayerModifier.AddItem(
                            avatarAddress,
                            eItem.ItemId,
                            eItem.RequiredBlockIndex,
                            1,
                            false);
                    }
                    break;
                case UnloadFromMyGaragesRecipientMail unloadFromMyGaragesRecipientMail:
                    if (unloadFromMyGaragesRecipientMail.FungibleIdAndCounts is not null)
                    {
                        var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                        foreach (var (fungibleId, fungibleCount) in
                                 unloadFromMyGaragesRecipientMail.FungibleIdAndCounts)
                        {
                            var row = materialSheet.OrderedList!
                                .FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                            if (row is null)
                            {
                                Debug.LogWarning($"Not found material sheet row. {fungibleId}");
                                continue;
                            }

                            var material = ItemFactory.CreateMaterial(row);
                            mailRewards.Add(new MailReward(material, fungibleCount));
                        }
                    }
                    ReactiveAvatarState.UpdateMailBox(Game.Game.instance.States.CurrentAvatarState.mailBox);
                    break;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            LocalMailHelper.Instance.ObservableMailBox
                .Subscribe(SetList)
                .AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateNotification)
                .AddTo(gameObject);

            emptyText.text = L10nManager.Localize(emptyTextL10nKey);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            MailBox = LocalMailHelper.Instance.MailBox;
            _toggleGroup.SetToggledOffAll();
            allButton.SetToggledOn();
            loading.SetActive(false);
            ChangeState(0);
            UpdateTabs();
            base.Show(ignoreShowAnimation);
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (MailTabState)state;

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            UpdateMailList(blockIndex);
        }

        private IEnumerable<Mail> GetAvailableMailList(long blockIndex,
            MailTabState state)
        {
            bool Predicate(Mail mail) => state switch
            {
                MailTabState.All => true,
                MailTabState.Workshop => mail.MailType is MailType.Grinding or MailType.Workshop,
                _ => mail.MailType == (MailType)state
            };
            return MailBox?.Where(mail => mail.requiredBlockIndex <= blockIndex)
                .Where(Predicate)
                .OrderByDescending(mail => mail.New);
        }

        private void UpdateMailList(long blockIndex)
        {
            var list = GetAvailableMailList(blockIndex, tabState)?.ToList();

            if (list is null)
            {
                return;
            }

            scroll.UpdateData(list, true);
            emptyImage.SetActive(!list.Any());
            UpdateTabs(blockIndex);
        }

        private void OnReceivedTutorialEquipment()
        {
            //It is not currently used, but but might use it later
        }

        public void UpdateTabs(long? blockIndex = null)
        {
            blockIndex ??= Game.Game.instance.Agent.BlockIndex;

            var isNew = MailBox.Any(mail => mail.New && mail.requiredBlockIndex <= blockIndex);
            allButton.HasNotification.Value = isNew;
            receiveAllContainer.SetActive(MailBox.Any(IsReceivableMail));
            Find<HeaderMenuStatic>().UpdateMailNotification(isNew);

            var list = GetAvailableMailList(blockIndex.Value, MailTabState.Workshop);
            var recent = list?.FirstOrDefault();
            workshopButton.HasNotification.Value = recent is { New: true };

            list = GetAvailableMailList(blockIndex.Value, MailTabState.Market);
            recent = list?.FirstOrDefault();
            marketButton.HasNotification.Value = recent is { New: true };

            list = GetAvailableMailList(blockIndex.Value, MailTabState.System);
            recent = list?.FirstOrDefault();
            systemButton.HasNotification.Value = recent is { New: true };
        }

        private bool IsReceivableMail(Mail mail)
        {
            return mail.New &&
                   mail.requiredBlockIndex <= Game.Game.instance.Agent.BlockIndex &&
                   mail is ProductBuyerMail or
                       ProductCancelMail or
                       OrderBuyerMail or
                       OrderSellerMail or
                       OrderExpirationMail or
                       CancelOrderMail or
                       CombinationMail or
                       ItemEnhanceMail or
                       UnloadFromMyGaragesRecipientMail;
        }

        private void SetList(MailBox mailBox)
        {
            if (mailBox is null)
            {
                return;
            }

            MailBox = mailBox;
            ChangeState((int)tabState);
        }

        private void UpdateNotification(long blockIndex)
        {
            MailBox = LocalMailHelper.Instance.MailBox;
            if (MailBox is null)
            {
                return;
            }

            UpdateTabs(blockIndex);
        }

        public void Read(CombinationMail mail)
        {
            var itemUsable = mail?.attachment?.itemUsable;
            if (itemUsable is null)
            {
                Debug.LogError("CombinationMail.itemUsable is null");
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // LocalLayer
            UniTask.Run(async () =>
            {
                LocalLayerModifier.AddItem(
                    avatarAddress,
                    itemUsable.ItemId,
                    itemUsable.RequiredBlockIndex,
                    1,
                    false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id, false);
                var (exist, avatarState) = await States.TryGetAvatarStateAsync(avatarAddress);
                if (!exist)
                {
                    return null;
                }

                return avatarState;
            }).ToObservable().SubscribeOnMainThread().Subscribe(async avatarState =>
            {
                Debug.Log("CombinationMail LocalLayer task completed");
                await States.Instance.AddOrReplaceAvatarStateAsync(avatarState,
                    States.Instance.CurrentAvatarKey);
            });
            // ~LocalLayer

            if (mail.attachment is CombinationConsumable5.ResultModel resultModel)
            {
                if (resultModel.subRecipeId.HasValue &&
                    Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                        resultModel.subRecipeId.Value,
                        out var row))
                {
                    Find<CombinationResultPopup>().Show(itemUsable, row.Options.Count);
                }
                else
                {
                    Find<CombinationResultPopup>().Show(itemUsable);
                }
            }
        }

        public async void Read(OrderBuyerMail orderBuyerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var order = await Util.GetOrder(orderBuyerMail.OrderId);
            var itemBase =
                await Util.GetItemBaseByTradableId(order.TradableId, order.ExpiredBlockIndex);
            var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
            var popup = Find<BuyItemInformationPopup>();
            var model = new UI.Model.BuyItemInformationPopup(new CountableItem(itemBase, count))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex,
                    count);
                LocalLayerModifier.RemoveNewMail(avatarAddress, orderBuyerMail.id, true);
            }).AddTo(gameObject);
            popup.Pop(model);
        }

        public async void Read(OrderSellerMail orderSellerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var order = await Util.GetOrder(orderSellerMail.OrderId);
            var taxedPrice = order.Price - order.GetTax();
            LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, taxedPrice).Forget();
            LocalLayerModifier.RemoveNewMail(avatarAddress, orderSellerMail.id);
        }

        public void Read(GrindingMail grindingMail)
        {
            Debug.Log($"[{nameof(GrindingMail)}] ItemCount: {grindingMail.ItemCount}, Asset: {grindingMail.Asset}");
        }

        public void Read(MaterialCraftMail materialCraftMail)
        {
            Debug.Log($"[{nameof(MaterialCraftMail)}] ItemCount: {materialCraftMail.ItemCount}, ItemId: {materialCraftMail.ItemId}");
        }

        public async void Read(ProductBuyerMail productBuyerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var productId = productBuyerMail.ProductId;
            var (_, itemProduct, favProduct) = await Game.Game.instance.MarketServiceClient.GetProductInfo(productId);
            if (itemProduct is not null)
            {
                var count = (int)itemProduct.Quantity;
                var item = States.Instance.CurrentAvatarState.inventory.Items
                    .FirstOrDefault(i => i.item is ITradableItem item &&
                                         item.TradableId.Equals(itemProduct.TradableId));
                if (item is null || item.item is null)
                {
                    return;
                }

                var model = new UI.Model.BuyItemInformationPopup(new CountableItem(item.item, count))
                {
                    isSuccess = true,
                    materialItems = new List<CombinationMaterial>()
                };

                model.OnClickSubmit.Subscribe(_ =>
                {
                    LocalLayerModifier.RemoveNewMail(avatarAddress, productBuyerMail.id, true);
                }).AddTo(gameObject);
                Find<BuyItemInformationPopup>().Pop(model);
            }

            if (favProduct is not null)
            {
                var currency = Currency.Legacy(favProduct.Ticker, 0, null);
                var fav = new FungibleAssetValue(currency, (int)favProduct.Quantity, 0);
                Find<BuyFungibleAssetInformationPopup>().Show(
                    fav,
                    () => LocalLayerModifier.RemoveNewMail(avatarAddress, productBuyerMail.id, true));
            }
        }

        public async void Read(ProductSellerMail productSellerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var (_, itemProduct, favProduct) = await Game.Game.instance.MarketServiceClient.GetProductInfo(productSellerMail.ProductId);
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            var price = itemProduct?.Price ?? favProduct.Price;
            var fav = new FungibleAssetValue(currency, (int)price, 0);
            var taxedPrice = fav.DivRem(100, out _) * Action.Buy.TaxRate;
            LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, taxedPrice).Forget();
            LocalLayerModifier.RemoveNewMail(avatarAddress, productSellerMail.id);
        }

        public void Read(ProductCancelMail productCancelMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Find<OneButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    LocalLayerModifier.RemoveNewMail(avatarAddress, productCancelMail.id);
                    ReactiveShopState.SetSellProducts();
                });
        }

        public async void Read(OrderExpirationMail orderExpirationMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var order = await Util.GetOrder(orderExpirationMail.OrderId);

            Find<OneButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    LocalLayerModifier.AddItem(avatarAddress, order.TradableId,
                        order.ExpiredBlockIndex, 1);
                    LocalLayerModifier.RemoveNewMail(avatarAddress, orderExpirationMail.id);
                });
        }

        public async void Read(CancelOrderMail cancelOrderMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var order = await Util.GetOrder(cancelOrderMail.OrderId);

            Find<OneButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    LocalLayerModifier.AddItem(avatarAddress, order.TradableId,
                        order.ExpiredBlockIndex, 1);
                    LocalLayerModifier.RemoveNewMail(avatarAddress, cancelOrderMail.id);
                    ReactiveShopState.SetSellProducts();
                });
        }

        public void Read(ItemEnhanceMail itemEnhanceMail)
        {
            var itemUsable = itemEnhanceMail?.attachment?.itemUsable;
            if (itemUsable is null)
            {
                Debug.LogError("ItemEnhanceMail.itemUsable is null");
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;

            // LocalLayer
            UniTask.Run(async () =>
            {
                if (itemEnhanceMail.attachment is ItemEnhancement13.ResultModel result)
                {
                    await LocalLayerModifier.ModifyAgentCrystalAsync(
                        States.Instance.AgentState.address,
                        result.CRYSTAL.MajorUnit);
                }

                if (itemUsable.ItemSubType == ItemSubType.Aura)
                {
                    //Because aura is a tradable item, local removal fails and an exception is handled.
                    LocalLayerModifier.AddNonFungibleItem(
                        avatarAddress,
                        itemUsable.ItemId,
                        false);
                }
                else
                {
                    LocalLayerModifier.AddItem(
                        avatarAddress,
                        itemUsable.ItemId,
                        itemUsable.RequiredBlockIndex,
                        1,
                        false);
                }

                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, itemEnhanceMail.id,
                    false);
                var (exist, avatarState) = await States.TryGetAvatarStateAsync(avatarAddress);
                if (!exist)
                {
                    return null;
                }

                return avatarState;
            }).ToObservable().SubscribeOnMainThread().Subscribe(async avatarState =>
            {
                Debug.Log("ItemEnhanceMail LocalLayer task completed");
                await States.Instance.AddOrReplaceAvatarStateAsync(avatarState,
                    States.Instance.CurrentAvatarKey);
            });
            // ~LocalLayer

            Find<EnhancementResultPopup>().Show(itemEnhanceMail);
        }

        public void Read(DailyRewardMail dailyRewardMail)
        {
            // ignored.
        }

        public void Read(MonsterCollectionMail monsterCollectionMail)
        {
            if (!(monsterCollectionMail.attachment is MonsterCollectionResult
                    monsterCollectionResult))
            {
                return;
            }

            var popup = Find<MonsterCollectionRewardsPopup>();
            popup.OnClickSubmit.First().Subscribe(widget =>
            {
                // LocalLayer
                for (var i = 0; i < monsterCollectionResult.rewards.Count; i++)
                {
                    var rewardInfo = monsterCollectionResult.rewards[i];
                    if (!rewardInfo.ItemId.TryParseAsTradableId(
                            Game.Game.instance.TableSheets.ItemSheet,
                            out var tradableId))
                    {
                        continue;
                    }


                    if (!rewardInfo.ItemId.TryGetFungibleId(
                            Game.Game.instance.TableSheets.ItemSheet,
                            out var fungibleId))
                    {
                        continue;
                    }

                    var avatarState = States.Instance.CurrentAvatarState;
                    avatarState.inventory.TryGetFungibleItems(fungibleId, out var items);
                    var item = items.FirstOrDefault(x => x.item is ITradableItem);
                    if (item != null && item is ITradableItem tradableItem)
                    {
                        LocalLayerModifier.AddItem(monsterCollectionResult.avatarAddress,
                            tradableId,
                            tradableItem.RequiredBlockIndex,
                            rewardInfo.Quantity);
                    }
                }

                LocalLayerModifier.RemoveNewAttachmentMail(monsterCollectionResult.avatarAddress,
                    monsterCollectionMail.id, true);
                // ~LocalLayer

                widget.Close();
            });
            popup.Pop(monsterCollectionResult.rewards);
        }

        public void Read(RaidRewardMail raidRewardMail)
        {
            raidRewardMail.New = false;
        }

        public void Read(UnloadFromMyGaragesRecipientMail unloadFromMyGaragesRecipientMail)
        {
            Analyzer.Instance.Track(
                "Unity/MailBox/UnloadFromMyGaragesRecipientMail/ReceiveButton/Click");
            var game = Game.Game.instance;
            unloadFromMyGaragesRecipientMail.New = false;
            LocalLayerModifier.RemoveNewMail(
                game.States.CurrentAvatarState.address,
                unloadFromMyGaragesRecipientMail.id);
            ReactiveAvatarState.UpdateMailBox(game.States.CurrentAvatarState.mailBox);

            if (unloadFromMyGaragesRecipientMail.Memo != null && unloadFromMyGaragesRecipientMail.Memo.Contains("season_pass"))
            {
                var mailRewards = new List<MailReward>();
                if (unloadFromMyGaragesRecipientMail.FungibleIdAndCounts is not null)
                {
                    var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                    foreach (var (fungibleId, fungibleCount) in
                             unloadFromMyGaragesRecipientMail.FungibleIdAndCounts)
                    {
                        var row = materialSheet.OrderedList!
                            .FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                        if (row is null)
                        {
                            Debug.LogWarning($"Not found material sheet row. {fungibleId}");
                            continue;
                        }

                        var material = ItemFactory.CreateMaterial(row);
                        mailRewards.Add(new MailReward(material, fungibleCount));
                    }
                }
                UpdateTabs();
                Find<MailRewardScreen>().Show(mailRewards);
                return;
            }

            var showQueue = new Queue<System.Action>();
            if (unloadFromMyGaragesRecipientMail.FungibleAssetValues is not null)
            {
                foreach (var (balanceAddr, value) in
                         unloadFromMyGaragesRecipientMail.FungibleAssetValues)
                {
                    // TODO: Enqueue functions.
                }
            }

            if (unloadFromMyGaragesRecipientMail.FungibleIdAndCounts is not null)
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var itemTooltip = ItemTooltip.Find(ItemType.Material);
                foreach (var (fungibleId, count) in
                         unloadFromMyGaragesRecipientMail.FungibleIdAndCounts)
                {
                    var row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                    if (row is null)
                    {
                        Debug.LogWarning($"Not found material sheet row. {fungibleId}");
                        continue;
                    }

                    var material = ItemFactory.CreateMaterial(row);
                    showQueue.Enqueue(() => itemTooltip.Show(
                        material,
                        L10nManager.Localize("UI_OK"),
                        true,
                        () => UniTask.WaitWhile(itemTooltip.IsActive)
                            .ToObservable()
                            .Subscribe(_ => showQueue.Dequeue()?.Invoke()),
                        itemCount: count)
                    );
                }
            }

            showQueue.Dequeue()?.Invoke();
        }

        public void TutorialActionClickFirstCombinationMailSubmitButton()
        {
            if (MailBox.Count == 0)
            {
                Debug.LogError(
                    "TutorialActionClickFirstCombinationMailSubmitButton() MailBox.Count == 0");
                return;
            }

            var mail = MailBox[0] as CombinationMail;
            if (mail is null)
            {
                Debug.LogError(
                    "TutorialActionClickFirstCombinationMailSubmitButton() mail is null");
                return;
            }

            Read(mail);
        }

        [Obsolete]
        public void Read(SellCancelMail mail)
        {
        }

        [Obsolete]
        public void Read(BuyerMail buyerMail)
        {
        }

        [Obsolete]
        public void Read(SellerMail sellerMail)
        {
        }
    }
}
