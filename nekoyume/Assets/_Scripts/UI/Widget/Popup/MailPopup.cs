using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.ApiClient;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Market;
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
    using Blockchain;
    using System.Text.RegularExpressions;
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

        private readonly Module.ToggleGroup _toggleGroup = new();

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
                    case ClaimItemsMail:
                    case CustomCraftMail:
                        LocalLayerModifier.RemoveNewMail(avatarAddress, mail.id);
                        break;
                    case ItemEnhanceMail:
                    case CombinationMail:
                        LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id);
                        break;
                }

                // 레이어 제거전에 New값을 바꾸면 루프안에서 레이어가 제거되지 않음
                mail.New = false;
            }

            loading.SetActive(false);
            ChangeState(0);
            UpdateTabs();
            Find<MailRewardScreen>().Show(mailRewards, "UI_ALL_RECEIVED");
        }

        private static async Task AddRewards(Mail mail, List<MailReward> mailRewards)
        {
            switch (mail)
            {
                case ProductBuyerMail productBuyerMail:
                    if (productBuyerMail.Product is ItemProduct itemProduct)
                    {
                        var item = States.Instance.CurrentAvatarState.inventory.Items
                            .FirstOrDefault(i => i.item is ITradableItem item &&
                                item.TradableId.Equals(itemProduct.TradableItem.TradableId));
                        if (item?.item is null)
                        {
                            return;
                        }

                        mailRewards.Add(new MailReward(item.item, itemProduct.ItemCount, true));
                    }

                    if (productBuyerMail.Product is FavProduct favProduct)
                    {
                        mailRewards.Add(new MailReward(favProduct.Asset, (int)favProduct.Asset.MajorUnit, true));
                    }

                    break;

                case ProductCancelMail productCancelMail:
                    var (_, cItemProduct, cFavProduct) = await ApiClients.Instance.MarketServiceClient.GetProductInfo(productCancelMail.ProductId);
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
                    }

                    break;

                case ItemEnhanceMail itemEnhanceMail:
                    var eItem = itemEnhanceMail?.attachment?.itemUsable;
                    if (eItem is not null)
                    {
                        mailRewards.Add(new MailReward(eItem, 1));
                    }

                    break;
                case UnloadFromMyGaragesRecipientMail unloadFromMyGaragesRecipientMail:
                    if (unloadFromMyGaragesRecipientMail.FungibleAssetValues is not null)
                    {
                        mailRewards.AddRange(
                            unloadFromMyGaragesRecipientMail.FungibleAssetValues.Select(fav =>
                                new MailReward(fav.value, (int)fav.value.MajorUnit)));
                    }

                    if (unloadFromMyGaragesRecipientMail.FungibleIdAndCounts is not null)
                    {
                        var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                        var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                        foreach (var (fungibleId, fungibleCount) in
                            unloadFromMyGaragesRecipientMail.FungibleIdAndCounts)
                        {
                            var row = materialSheet.OrderedList!.FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                            if (row != null)
                            {
                                var material = ItemFactory.CreateMaterial(row);
                                mailRewards.Add(new MailReward(material, fungibleCount));
                                continue;
                            }

                            row = materialSheet.OrderedList!.FirstOrDefault(row => row.Id.Equals(fungibleId));
                            if (row != null)
                            {
                                var material = ItemFactory.CreateMaterial(row);
                                mailRewards.Add(new MailReward(material, fungibleCount));
                                continue;
                            }

                            var itemRow = itemSheet.OrderedList!.FirstOrDefault(row => row.Equals(fungibleId));
                            if (itemRow != null)
                            {
                                var item = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                                mailRewards.Add(new MailReward(item, fungibleCount));
                                continue;
                            }

                            NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");
                        }
                    }

                    ReactiveAvatarState.UpdateMailBox(Game.Game.instance.States.CurrentAvatarState.mailBox);
                    break;
                case ClaimItemsMail claimItemsMail:
                    if (claimItemsMail.FungibleAssetValues is not null)
                    {
                        mailRewards.AddRange(
                            claimItemsMail.FungibleAssetValues.Select(fav =>
                                new MailReward(fav, fav.MajorUnit)));
                    }

                    if (claimItemsMail.Items is not null)
                    {
                        var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                        var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                        foreach (var (fungibleId, itemCount) in
                            claimItemsMail.Items)
                        {
                            var row = materialSheet.OrderedList!
                                .FirstOrDefault(row => row.Id.Equals(fungibleId));
                            if (row != null)
                            {
                                var material = ItemFactory.CreateMaterial(row);
                                mailRewards.Add(new MailReward(material, itemCount));
                                continue;
                            }

                            row = materialSheet.OrderedList!.FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                            if (row != null)
                            {
                                var material = ItemFactory.CreateMaterial(row);
                                mailRewards.Add(new MailReward(material, itemCount));
                                continue;
                            }

                            if (itemSheet.TryGetValue(fungibleId, out var itemSheetRow))
                            {
                                var item = ItemFactory.CreateItem(itemSheetRow, new ActionRenderHandler.LocalRandom(0));
                                mailRewards.Add(new MailReward(item, itemCount));
                                continue;
                            }

                            NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");
                        }
                    }

                    ReactiveAvatarState.UpdateMailBox(Game.Game.instance.States.CurrentAvatarState.mailBox);
                    break;
                case CustomCraftMail customCraftMail:
                    var equipment = customCraftMail.Equipment;
                    if (equipment is not null)
                    {
                        mailRewards.Add(new MailReward(equipment, 1));
                    }

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
            bool Predicate(Mail mail)
            {
                return state switch
                {
                    MailTabState.All => true,
                    MailTabState.Workshop => mail.MailType is MailType.Grinding or MailType.Workshop or MailType.CustomCraft,
                    _ => mail.MailType == (MailType)state
                };
            }

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
                    UnloadFromMyGaragesRecipientMail or
                    ClaimItemsMail or
                    CustomCraftMail;
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
                NcDebug.LogError("CombinationMail.itemUsable is null");
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id);
            mail.New = false;
            NcDebug.Log("CombinationMail LocalLayer task completed");
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);

            if (mail.attachment is CombinationConsumable5.ResultModel resultModel)
            {
                if (resultModel.subRecipeId.HasValue &&
                    Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                        resultModel.subRecipeId.Value,
                        out var row))
                {
                    Find<CombinationResultScreen>().Show(itemUsable, row.Options.Count);
                }
                else
                {
                    Find<CombinationResultScreen>().Show(itemUsable);
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
                orderBuyerMail.New = false;
                LocalLayerModifier.RemoveNewMail(avatarAddress, orderBuyerMail.id);
                ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
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
            orderSellerMail.New = false;
            LocalLayerModifier.RemoveNewMail(avatarAddress, orderSellerMail.id);
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        public void Read(GrindingMail grindingMail)
        {
            NcDebug.Log($"[{nameof(GrindingMail)}] ItemCount: {grindingMail.ItemCount}, Asset: {grindingMail.Asset}");
            grindingMail.New = false;
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        public void Read(MaterialCraftMail materialCraftMail)
        {
            NcDebug.Log($"[{nameof(MaterialCraftMail)}] ItemCount: {materialCraftMail.ItemCount}, ItemId: {materialCraftMail.ItemId}");
            materialCraftMail.New = false;
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        public void Read(ProductBuyerMail productBuyerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (productBuyerMail.Product is ItemProduct itemProduct)
            {
                var count = itemProduct.ItemCount;
                var item = States.Instance.CurrentAvatarState.inventory.Items
                    .FirstOrDefault(i => i.item is ITradableItem item &&
                        item.TradableId.Equals(itemProduct.TradableItem.TradableId));
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
                    productBuyerMail.New = false;
                    LocalLayerModifier.RemoveNewMail(avatarAddress, productBuyerMail.id);
                    ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
                }).AddTo(gameObject);
                Find<BuyItemInformationPopup>().Pop(model);
                return;
            }

            if (productBuyerMail.Product is FavProduct favProduct)
            {
                var currency = Currency.Legacy(favProduct.Asset.Currency.Ticker, 0, null);
                var fav = new FungibleAssetValue(currency, (int)favProduct.Asset.MajorUnit, 0);
                Find<BuyFungibleAssetInformationPopup>().Show(
                    fav,
                    () =>
                    {
                        productBuyerMail.New = false;
                        LocalLayerModifier.RemoveNewMail(avatarAddress, productBuyerMail.id);
                        ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
                    });
                return;
            }

            productBuyerMail.New = false;
            LocalLayerModifier.RemoveNewMail(avatarAddress, productBuyerMail.id);
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        public void Read(ProductSellerMail productSellerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var fav = productSellerMail.Product.Price;
            var taxedPrice = fav.DivRem(100, out _) * Buy.TaxRate;
            LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, taxedPrice).Forget();
            productSellerMail.New = false;
            LocalLayerModifier.RemoveNewMail(avatarAddress, productSellerMail.id);
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        public void Read(ProductCancelMail productCancelMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Find<OneButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    productCancelMail.New = false;
                    LocalLayerModifier.RemoveNewMail(avatarAddress, productCancelMail.id);
                    ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
                    ReactiveShopState.SetSellProducts();
                });
        }

        public void Read(OrderExpirationMail orderExpirationMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Find<OneButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    orderExpirationMail.New = false;
                    LocalLayerModifier.RemoveNewMail(avatarAddress, orderExpirationMail.id);
                    ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
                });
        }

        public void Read(CancelOrderMail cancelOrderMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Find<OneButtonSystem>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    cancelOrderMail.New = false;
                    LocalLayerModifier.RemoveNewMail(avatarAddress, cancelOrderMail.id);
                    ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
                    ReactiveShopState.SetSellProducts();
                });
        }

        public void Read(ItemEnhanceMail itemEnhanceMail)
        {
            var itemUsable = itemEnhanceMail?.attachment?.itemUsable;
            if (itemUsable is null)
            {
                NcDebug.LogError("ItemEnhanceMail.itemUsable is null");
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

                itemEnhanceMail.New = false;
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, itemEnhanceMail.id);
                ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
            }).ToObservable().SubscribeOnMainThread().Subscribe(_ => { NcDebug.Log("ItemEnhanceMail LocalLayer task completed"); });
            // ~LocalLayer

            Find<EnhancementResultScreen>().Show(itemEnhanceMail);
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
                LocalLayerModifier.RemoveNewAttachmentMail(monsterCollectionResult.avatarAddress,
                    monsterCollectionMail.id);
                ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);

                widget.Close();
            });
            popup.Pop(monsterCollectionResult.rewards);
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
            NcDebug.Log($"[MailRead] MailPopupReadUnloadFromMyGaragesRecipientMail mailid : {unloadFromMyGaragesRecipientMail.id} Memo : {unloadFromMyGaragesRecipientMail.Memo}");

            if (unloadFromMyGaragesRecipientMail.Memo != null && unloadFromMyGaragesRecipientMail.Memo.Contains("season_pass"))
            {
                var mailRewards = new List<MailReward>();
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;

                var iapProductFindComplete = false;
                if (unloadFromMyGaragesRecipientMail.Memo.Contains("iap"))
                {
                    var sku = MailExtensions.GetSkuFromMemo(unloadFromMyGaragesRecipientMail.Memo);
                    if (!string.IsNullOrEmpty(sku))
                    {
                        var findKey = Game.Game.instance.IAPStoreManager.SeasonPassProduct.FirstOrDefault(_ => _.Value.CheckSku(sku));
                        if (findKey.Value != null)
                        {
                            iapProductFindComplete = true;
                            foreach (var item in findKey.Value.FungibleItemList)
                            {
                                var row = materialSheet.OrderedList!
                                    .FirstOrDefault(row => row.ItemId.Equals(item.FungibleItemId));
                                if (row is null)
                                {
                                    NcDebug.LogWarning($"Not found material sheet row. {item.FungibleItemId}");
                                    continue;
                                }

                                var material = ItemFactory.CreateMaterial(row);
                                mailRewards.Add(new MailReward(material, item.Amount));
                            }

                            foreach (var item in findKey.Value.FavList)
                            {
                                var currency = Currency.Legacy(item.Ticker, 0, null);
                                var fav = new FungibleAssetValue(currency, (int)item.Amount, 0);
                                mailRewards.Add(new MailReward(fav, (int)item.Amount, true));
                            }
                        }
                    }
                }

                if (unloadFromMyGaragesRecipientMail.FungibleIdAndCounts is not null && !iapProductFindComplete)
                {
                    foreach (var (fungibleId, fungibleCount) in
                        unloadFromMyGaragesRecipientMail.FungibleIdAndCounts)
                    {
                        var row = materialSheet.OrderedList!
                            .FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                        if (row is null)
                        {
                            NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");
                            continue;
                        }

                        var material = ItemFactory.CreateMaterial(row);
                        mailRewards.Add(new MailReward(material, fungibleCount));
                    }

                    foreach (var (add, fav) in unloadFromMyGaragesRecipientMail.FungibleAssetValues)
                    {
                        mailRewards.Add(new MailReward(fav, fav.MajorUnit, true));
                    }
                }

                UpdateTabs();
                Find<MailRewardScreen>().Show(mailRewards, "UI_IAP_PURCHASE_DELIVERY_COMPLETE_POPUP_TITLE");
                return;
            }

            var rewards = new List<MailReward>();
            if (unloadFromMyGaragesRecipientMail.FungibleAssetValues is not null)
            {
                rewards.AddRange(
                    unloadFromMyGaragesRecipientMail.FungibleAssetValues.Select(fav =>
                        new MailReward(fav.value, fav.value.MajorUnit)));
            }

            if (unloadFromMyGaragesRecipientMail.FungibleIdAndCounts is not null)
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var (fungibleId, count) in
                    unloadFromMyGaragesRecipientMail.FungibleIdAndCounts)
                {
                    var row = materialSheet.OrderedList!.FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        rewards.Add(new MailReward(material, count));
                        continue;
                    }

                    row = materialSheet.OrderedList!.FirstOrDefault(row => row.Id.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        rewards.Add(new MailReward(material, count));
                        continue;
                    }

                    var itemRow = itemSheet.OrderedList!.FirstOrDefault(row => row.Equals(fungibleId));
                    if (itemRow != null)
                    {
                        var item = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                        rewards.Add(new MailReward(item, count));
                        continue;
                    }
                }
            }

            UpdateTabs();
            Find<MailRewardScreen>().Show(rewards, "UI_IAP_PURCHASE_DELIVERY_COMPLETE_POPUP_TITLE");
            return;
        }

        public void Read(ClaimItemsMail claimItemsMail)
        {
            Analyzer.Instance.Track(
                "Unity/MailBox/ClaimItemsMail/ReceiveButton/Click");

            var game = Game.Game.instance;
            claimItemsMail.New = false;
            LocalLayerModifier.RemoveNewMail(
                game.States.CurrentAvatarState.address,
                claimItemsMail.id);
            ReactiveAvatarState.UpdateMailBox(game.States.CurrentAvatarState.mailBox);
            NcDebug.Log($"[MailRead] MailPopupReadClaimItemsMail mailid : {claimItemsMail.id} Memo : {claimItemsMail.Memo}");

            var rewards = new List<MailReward>();
            if (claimItemsMail.FungibleAssetValues is not null)
            {
                rewards.AddRange(
                    claimItemsMail.FungibleAssetValues.Select(fav =>
                        new MailReward(fav, fav.MajorUnit)));
            }

            if (claimItemsMail.Items is not null)
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var (fungibleId, count) in
                    claimItemsMail.Items)
                {
                    var row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.Id.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        rewards.Add(new MailReward(material, count));
                        continue;
                    }

                    row = materialSheet.OrderedList!.FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        rewards.Add(new MailReward(material, count));
                        continue;
                    }

                    if (itemSheet.TryGetValue(fungibleId, out var itemSheetRow))
                    {
                        var item = ItemFactory.CreateItem(itemSheetRow, new ActionRenderHandler.LocalRandom(0));
                        rewards.Add(new MailReward(item, 1));
                        continue;
                    }

                    NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");
                }
            }

            Find<MailRewardScreen>().Show(rewards, "UI_IAP_PURCHASE_DELIVERY_COMPLETE_POPUP_TITLE");
        }

        public void TutorialActionClickFirstCombinationMailSubmitButton()
        {
            if (MailBox.Count == 0)
            {
                NcDebug.LogError(
                    "TutorialActionClickFirstCombinationMailSubmitButton() MailBox.Count == 0");
                return;
            }

            var mail = MailBox[0] as CombinationMail;
            if (mail is null)
            {
                NcDebug.LogError(
                    "TutorialActionClickFirstCombinationMailSubmitButton() mail is null");
                return;
            }

            Read(mail);
        }

        public void Read(AdventureBossRaffleWinnerMail adventureBossRaffleWinnerMail)
        {
            NcDebug.Log($"[{nameof(AdventureBossRaffleWinnerMail)}] ItemCount: {adventureBossRaffleWinnerMail.id}, Season: {adventureBossRaffleWinnerMail.Season}, ");
            adventureBossRaffleWinnerMail.New = false;
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
        }

        public void Read(CustomCraftMail customCraftMail)
        {
            var itemUsable = customCraftMail?.Equipment;
            if (itemUsable is null)
            {
                NcDebug.LogError("CombinationMail.itemUsable is null");
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            LocalLayerModifier.RemoveNewMail(avatarAddress, customCraftMail.id);
            customCraftMail.New = false;
            NcDebug.Log("CombinationMail LocalLayer task completed");
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
            Find<CustomCraftResultScreen>().Show(itemUsable);
        }

        public void Read(PatrolRewardMail patrolRewardMail)
        {
            var game = Game.Game.instance;
            patrolRewardMail.New = false;
            var avatarAddress = game.States.CurrentAvatarState.address;
            LocalLayerModifier.RemoveNewMail(avatarAddress, patrolRewardMail.id);
            ReactiveAvatarState.UpdateMailBox(game.States.CurrentAvatarState.mailBox);

            NcDebug.Log($"[MailRead] MailPopupRead PatrolRewardMail mailid : {patrolRewardMail.id}");

            var rewards = new List<MailReward>();
            if (patrolRewardMail.FungibleAssetValues is not null)
            {
                rewards.AddRange(
                    patrolRewardMail.FungibleAssetValues.Select(fav =>
                        new MailReward(fav, fav.MajorUnit)));
            }

            if (patrolRewardMail.Items is not null)
            {
                var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
                var itemSheet = Game.Game.instance.TableSheets.ItemSheet;
                foreach (var (fungibleId, count) in
                    patrolRewardMail.Items)
                {
                    var row = materialSheet.OrderedList!
                        .FirstOrDefault(row => row.Id.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        rewards.Add(new MailReward(material, count));
                        continue;
                    }

                    row = materialSheet.OrderedList!.FirstOrDefault(row => row.ItemId.Equals(fungibleId));
                    if (row != null)
                    {
                        var material = ItemFactory.CreateMaterial(row);
                        rewards.Add(new MailReward(material, count));
                        continue;
                    }

                    if (itemSheet.TryGetValue(fungibleId, out var itemSheetRow))
                    {
                        var item = ItemFactory.CreateItem(itemSheetRow, new ActionRenderHandler.LocalRandom(0));
                        rewards.Add(new MailReward(item, 1));
                        continue;
                    }

                    NcDebug.LogWarning($"Not found material sheet row. {fungibleId}");
                }
            }

            Find<MailRewardScreen>().Show(rewards, "UI_IAP_PURCHASE_DELIVERY_COMPLETE_POPUP_TITLE");
        }

        public void Read(RaidRewardMail raidRewardMail)
        {
            raidRewardMail.New = false;
            ReactiveAvatarState.UpdateMailBox(States.Instance.CurrentAvatarState.mailBox);
            NcDebug.Log($"[MailRead] MailPopupReadRaidRewardMail mailid : {raidRewardMail.id}");
        }

        [Obsolete]
        public void Read(WorldBossRewardMail worldBossRewardMail)
        {
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
