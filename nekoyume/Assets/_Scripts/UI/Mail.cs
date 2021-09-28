using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Model.Order;
using Nekoyume.Action;
using Nekoyume.EnumType;
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

    public class Mail : XTweenWidget, IMail
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
        private Blur blur = null;

        [SerializeField]
        private Button closeButton = null;

        private readonly Module.ToggleGroup _toggleGroup = new Module.ToggleGroup();

        private static Sprite _selectedButtonSprite;

        private const int TutorialEquipmentId = 10110000;

        public MailBox MailBox { get; private set; }

        public override WidgetType WidgetType => WidgetType.Popup;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;

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

            if (blur)
            {
                blur.Show();
            }
            HelpPopup.HelpMe(100010, true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur && blur.isActiveAndEnabled)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (MailTabState) state;

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            UpdateMailList(blockIndex);
        }

        private IEnumerable<Nekoyume.Model.Mail.Mail> GetAvailableMailList(long blockIndex, MailTabState state)
        {
            bool predicate(Nekoyume.Model.Mail.Mail mail)
            {
                if (state == MailTabState.All)
                {
                    return true;
                }

                return mail.MailType == (MailType) state;
            }

            return MailBox?.Where(mail =>
                mail.requiredBlockIndex <= blockIndex)
                .Where(predicate)
                .OrderByDescending(mail => mail.New);
        }

        private void UpdateMailList(long blockIndex)
        {
            var list = GetAvailableMailList(blockIndex, tabState);

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
            var tutorialController = Game.Game.instance.Stage.TutorialController;
            var tutorialProgress = tutorialController.GetTutorialProgress();
            if (tutorialController.CurrentlyPlayingId < 37)
            {
                tutorialController.Stop(() => tutorialController.Play(37));
            }
        }

        public void UpdateTabs(long? blockIndex = null)
        {
            blockIndex ??= Game.Game.instance.Agent.BlockIndex;

            // 전체 탭
            allButton.HasNotification.Value = MailBox
                .Any(mail => mail.New && mail.requiredBlockIndex <= blockIndex);

            var list = GetAvailableMailList(blockIndex.Value, MailTabState.Workshop);
            var recent = list?.FirstOrDefault();
            workshopButton.HasNotification.Value = recent is null ? false : recent.New;

            list = GetAvailableMailList(blockIndex.Value, MailTabState.Market);
            recent = list?.FirstOrDefault();
            marketButton.HasNotification.Value = recent is null ? false : recent.New;

            list = GetAvailableMailList(blockIndex.Value, MailTabState.System);
            recent = list?.FirstOrDefault();
            systemButton.HasNotification.Value = recent is null ? false : recent.New;
        }

        private void SetList(MailBox mailBox)
        {
            if (mailBox is null)
            {
                return;
            }

            MailBox = mailBox;
            ChangeState((int) tabState);
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
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (CombinationConsumable5.ResultModel) mail.attachment;
            if (attachment.itemUsable is null)
            {
                Debug.LogError("CombinationMail.itemUsable is null");
                return;
            }

            var itemUsable = attachment.itemUsable;

            // LocalLayer
            UniTask.Run(() =>
            {
                LocalLayerModifier.AddItem(
                    avatarAddress,
                    itemUsable.TradableId,
                    itemUsable.RequiredBlockIndex,
                    1,
                    false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id, false);
                if (!States.TryGetAvatarState(avatarAddress, out var avatarState))
                {
                    return null;
                }

                return avatarState;
            }).ToObservable().SubscribeOnMainThread().Subscribe(avatarState =>
            {
                Debug.Log("CombinationMail LocalLayer task completed");
                States.Instance.AddOrReplaceAvatarState(avatarState, States.Instance.CurrentAvatarKey);
            });
            // ~LocalLayer

            if (mail.attachment is CombinationConsumable5.ResultModel resultModel &&
                resultModel.subRecipeId.HasValue &&
                Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                    resultModel.subRecipeId.Value,
                    out var row))
            {
                Find<CombinationResult>().Show(itemUsable, row.Options.Count);
            }
        }

        public void Read(OrderBuyerMail orderBuyerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var order = Util.GetOrder(orderBuyerMail.OrderId);
            var itemBase = Util.GetItemBaseByTradableId(order.TradableId, order.ExpiredBlockIndex);
            var count = order is FungibleOrder fungibleOrder ? fungibleOrder.ItemCount : 1;
            var popup = Find<CombinationResultPopup>();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(itemBase, count))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, count);
                LocalLayerModifier.RemoveNewMail(avatarAddress, orderBuyerMail.id, true);
            }).AddTo(gameObject);
            popup.Pop(model);
        }

        public void Read(OrderSellerMail orderSellerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var order = Util.GetOrder(orderSellerMail.OrderId);
            var taxedPrice = order.Price - order.GetTax();
            LocalLayerModifier.ModifyAgentGold(agentAddress, taxedPrice);
            LocalLayerModifier.RemoveNewMail(avatarAddress, orderSellerMail.id);
        }

        public void Read(OrderExpirationMail orderExpirationMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var order = Util.GetOrder(orderExpirationMail.OrderId);

            Find<OneButtonPopup>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    LocalLayerModifier.AddItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, 1);
                    LocalLayerModifier.RemoveNewMail(avatarAddress, orderExpirationMail.id);
                });
        }

        public void Read(CancelOrderMail cancelOrderMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var order = Util.GetOrder(cancelOrderMail.OrderId);

            Find<OneButtonPopup>().Show(L10nManager.Localize("UI_SELL_CANCEL_INFO"),
                L10nManager.Localize("UI_YES"),
                () =>
                {
                    LocalLayerModifier.AddItem(avatarAddress, order.TradableId, order.ExpiredBlockIndex, 1);
                    LocalLayerModifier.RemoveNewMail(avatarAddress, cancelOrderMail.id);
                    var shopSell = Find<ShopSell>();
                    if (shopSell.isActiveAndEnabled)
                    {
                        shopSell.Refresh();
                    }
                });
        }

        public void Read(ItemEnhanceMail itemEnhanceMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (ItemEnhancement.ResultModel) itemEnhanceMail.attachment;
            if (attachment.itemUsable is null)
            {
                Debug.LogError("ItemEnhanceMail.itemUsable is null");
                return;
            }

            var itemUsable = attachment.itemUsable;

            // LocalLayer
            UniTask.Run(() =>
            {
                LocalLayerModifier.AddItem(
                    avatarAddress,
                    itemUsable.TradableId,
                    itemUsable.RequiredBlockIndex,
                    1,
                    false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, itemEnhanceMail.id, false);
                if (!States.TryGetAvatarState(avatarAddress, out var avatarState))
                {
                    return null;
                }

                return avatarState;
            }).ToObservable().SubscribeOnMainThread().Subscribe(avatarState =>
            {
                Debug.Log("ItemEnhanceMail LocalLayer task completed");
                States.Instance.AddOrReplaceAvatarState(avatarState, States.Instance.CurrentAvatarKey);
            });
            // ~LocalLayer

            Find<EnhancementResult>().Show(itemEnhanceMail);
        }

        public void Read(DailyRewardMail dailyRewardMail)
        {
            // ignored.
        }

        public void Read(MonsterCollectionMail monsterCollectionMail)
        {
            if (!(monsterCollectionMail.attachment is MonsterCollectionResult monsterCollectionResult))
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

                LocalLayerModifier.RemoveNewAttachmentMail(monsterCollectionResult.avatarAddress, monsterCollectionMail.id, true);
                // ~LocalLayer

                widget.Close();
            });
            popup.Pop(monsterCollectionResult.rewards);
        }

        public void TutorialActionClickFirstCombinationMailSubmitButton()
        {
            if (MailBox.Count == 0)
            {
                Debug.LogError("TutorialActionClickFirstCombinationMailSubmitButton() MailBox.Count == 0");
                return;
            }

            var mail = MailBox[0] as CombinationMail;
            if (mail is null)
            {
                Debug.LogError("TutorialActionClickFirstCombinationMailSubmitButton() mail is null");
                return;
            }

            Read(mail);
        }

        [Obsolete]
        public void Read(SellCancelMail mail) { }

        [Obsolete]
        public void Read(BuyerMail buyerMail) { }

        [Obsolete]
        public void Read(SellerMail sellerMail) { }
    }
}
