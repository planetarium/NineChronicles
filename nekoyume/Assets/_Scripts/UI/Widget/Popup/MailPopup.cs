using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
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

    public class MailPopup : XTweenPopupWidget, IMail
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
        private MailScroll scroll = null;

        [SerializeField]
        private GameObject emptyImage = null;

        [SerializeField]
        private TextMeshProUGUI emptyText = null;

        [SerializeField]
        private string emptyTextL10nKey = null;

        [SerializeField]
        private Button closeButton = null;

        private readonly Module.ToggleGroup _toggleGroup = new Module.ToggleGroup();

        private static Sprite _selectedButtonSprite;

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
        }

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");

            ReactiveAvatarState.MailBox?.Subscribe(SetList).AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateNotification)
                .AddTo(gameObject);

            emptyText.text = L10nManager.Localize(emptyTextL10nKey);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            MailBox = States.Instance.CurrentAvatarState.mailBox;
            _toggleGroup.SetToggledOffAll();
            allButton.SetToggledOn();
            ChangeState(0);
            UpdateTabs();
            base.Show(ignoreShowAnimation);
            HelpTooltip.HelpMe(100010, true);
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (MailTabState)state;

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            UpdateMailList(blockIndex);
        }

        private IEnumerable<Nekoyume.Model.Mail.Mail> GetAvailableMailList(long blockIndex,
            MailTabState state)
        {
            bool predicate(Nekoyume.Model.Mail.Mail mail)
            {
                if (state == MailTabState.All)
                {
                    return true;
                }

                return mail.MailType == (MailType)state;
            }

            return MailBox?.Where(mail =>
                    mail.requiredBlockIndex <= blockIndex)
                .Where(predicate)
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

            // 전체 탭
            allButton.HasNotification.Value = MailBox
                .Any(mail => mail.New && mail.requiredBlockIndex <= blockIndex);

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
            var avatarState = States.Instance.CurrentAvatarState;
            if (avatarState is null)
            {
                return;
            }

            MailBox = avatarState.mailBox;
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
                    itemUsable.TradableId,
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
            LocalLayerModifier.ModifyAgentGold(agentAddress, taxedPrice);
            LocalLayerModifier.RemoveNewMail(avatarAddress, orderSellerMail.id);
        }

        public void Read(GrindingMail grindingMail)
        {
            Debug.Log($"[{nameof(GrindingMail)}] ItemCount: {grindingMail.ItemCount}, Asset: {grindingMail.Asset}");
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
                    ReactiveShopState.UpdateSellDigests();
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
                if (itemEnhanceMail.attachment is ItemEnhancement.ResultModel result)
                {
                    LocalLayerModifier.ModifyAgentCrystal(States.Instance.AgentState.address, result.CRYSTAL.MajorUnit);
                }

                LocalLayerModifier.AddItem(
                    avatarAddress,
                    itemUsable.TradableId,
                    itemUsable.RequiredBlockIndex,
                    1,
                    false);
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
