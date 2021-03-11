using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Mail : XTweenWidget, IMail
    {
        public enum MailTabState : int
        {
            All,
            Workshop,
            Auction,
            System
        }

        [Serializable]
        public class TabButton
        {
            private static readonly Vector2 LeftBottom = new Vector2(-15f, -10.5f);
            private static readonly Vector2 MinusRightTop = new Vector2(15f, 13f);

            public Sprite highlightedSprite;
            public Button button;
            public Image hasNotificationImage;
            public Image image;
            public Image icon;
            public TextMeshProUGUI text;
            public TextMeshProUGUI textSelected;

            public void Init(string localizationKey)
            {
                if (!button) return;
                var localized = L10nManager.Localize(localizationKey);
                var content = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(localized.ToLower());
                text.text = content;
                textSelected.text = content;
            }

            public void ChangeColor(bool isHighlighted = false)
            {
                image.overrideSprite = isHighlighted ? _selectedButtonSprite : null;
                var imageRectTransform = image.rectTransform;
                imageRectTransform.offsetMin = isHighlighted ? LeftBottom : Vector2.zero;
                imageRectTransform.offsetMax = isHighlighted ? MinusRightTop : Vector2.zero;
                icon.overrideSprite = isHighlighted ? highlightedSprite : null;
                text.gameObject.SetActive(!isHighlighted);
                textSelected.gameObject.SetActive(isHighlighted);
            }
        }

        [SerializeField]
        private MailTabState tabState = default;

        [SerializeField]
        private MailScroll scroll = null;

        [SerializeField]
        private TabButton[] tabButtons = null;

        [SerializeField]
        private GameObject emptyImage = null;

        [SerializeField]
        private TextMeshProUGUI emptyText = null;

        [SerializeField]
        private string emptyTextL10nKey = null;

        [SerializeField]
        private Blur blur = null;

        private static Sprite _selectedButtonSprite;

        private const int TutorialEquipmentId = 10110000;

        public MailBox MailBox { get; private set; }

        #region override

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");

            tabButtons[0].Init("ALL");
            tabButtons[1].Init("UI_COMBINATION");
            tabButtons[2].Init("UI_SHOP");
            tabButtons[3].Init("SYSTEM");
            ReactiveAvatarState.MailBox?.Subscribe(SetList).AddTo(gameObject);
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateMailList)
                .AddTo(gameObject);

            emptyText.text = L10nManager.Localize(emptyTextL10nKey);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            tabState = MailTabState.All;
            MailBox = States.Instance.CurrentAvatarState.mailBox;
            ChangeState(0);
            UpdateTabs();
            base.Show(ignoreShowAnimation);

            if (blur)
            {
                blur.Show();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (MailTabState)state;

            for (var i = 0; i < tabButtons.Length; ++i)
            {
                tabButtons[i].ChangeColor(i == state);
            }

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

            // Temporarily disable mail tutorial.
            //var tutorialController = Game.Game.instance.Stage.TutorialController;
            //var tutorialProgress = tutorialController.GetTutorialProgress();
            //if (tutorialProgress == 1)
            //{
            //    var tutorialEquipment = States.Instance.CurrentAvatarState.mailBox
            //    .FirstOrDefault(x =>
            //    x is AttachmentMail mail &&
            //    mail.requiredBlockIndex <= blockIndex &&
            //    !(mail.attachment.itemUsable is null) &&
            //    mail.attachment.itemUsable.ItemSubType == Nekoyume.Model.Item.ItemSubType.Weapon &&
            //    mail.attachment.itemUsable.Id == TutorialEquipmentId);
            //    if (!(tutorialEquipment is null))
            //    {
            //        OnReceivedTutorialEquipment();
            //    }
            //}
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
            if (blockIndex is null)
            {
                blockIndex = Game.Game.instance.Agent.BlockIndex;
            }

            // 전체 탭
            tabButtons[0].hasNotificationImage.enabled = MailBox
                .Any(mail => mail.New && mail.requiredBlockIndex <= blockIndex);

            for (var i = 1; i < tabButtons.Length; ++i)
            {
                var list = GetAvailableMailList(blockIndex.Value, (MailTabState) i);
                var recent = list?.FirstOrDefault();
                tabButtons[i].hasNotificationImage.enabled = recent is null ?
                    false : recent.New;
            }
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

        public void Read(CombinationMail mail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (CombinationConsumable.ResultModel) mail.attachment;
            var itemBase = attachment.itemUsable ?? (ItemBase)attachment.costume;
            var nonFungibleItem = attachment.itemUsable ?? (INonFungibleItem)attachment.costume;
            var popup = Find<CombinationResultPopup>();
            var materialItems = attachment.materials
                .Select(pair => new {pair, item = pair.Key})
                .Select(t => new CombinationMaterial(
                    t.item,
                    t.pair.Value,
                    t.pair.Value,
                    t.pair.Value))
                .ToList();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(itemBase, 1))
            {
                isSuccess = true,
                materialItems = materialItems
            };
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, nonFungibleItem.ItemId, false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id, false);
                LocalLayerModifier.RemoveAttachmentResult(avatarAddress, mail.id);
                LocalLayerModifier.ModifyAvatarItemRequiredIndex(
                    avatarAddress,
                    nonFungibleItem.ItemId,
                    Game.Game.instance.Agent.BlockIndex);
            });
            popup.Pop(model);
        }

        public void Read(SellCancelMail mail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (SellCancellation.Result) mail.attachment;
            var itemBase = attachment.itemUsable ?? (ItemBase)attachment.costume;
            var nonFungibleItem = attachment.itemUsable ?? (INonFungibleItem)attachment.costume;
            //TODO 관련 기획이 끝나면 별도 UI를 생성
            var popup = Find<ItemCountAndPricePopup>();
            var model = new UI.Model.ItemCountAndPricePopup();
            model.TitleText.Value = L10nManager.Localize("UI_RETRIEVE");
            model.InfoText.Value = L10nManager.Localize("UI_SELL_CANCEL_INFO");
            model.PriceInteractable.Value = false;
            model.Price.Value = attachment.shopItem.Price;
            model.CountEnabled.Value = false;
            model.Item.Value = new CountEditableItem(itemBase, 1, 1, 1);
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, nonFungibleItem.ItemId, false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id);
                popup.Close();
            }).AddTo(gameObject);
            model.OnClickCancel.Subscribe(_ =>
            {
                //TODO 재판매 처리추가되야함\
                LocalLayerModifier.AddItem(avatarAddress, nonFungibleItem.ItemId, false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, mail.id);
                popup.Close();
            }).AddTo(gameObject);
            popup.Pop(model);
        }

        public void Read(BuyerMail buyerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (Buy.BuyerResult) buyerMail.attachment;
            var itemBase = attachment.itemUsable ?? (ItemBase)attachment.costume;
            var nonFungibleItem = attachment.itemUsable ?? (INonFungibleItem)attachment.costume;
            var popup = Find<CombinationResultPopup>();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(itemBase, 1))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, nonFungibleItem.ItemId, false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, buyerMail.id);
            }).AddTo(gameObject);
            popup.Pop(model);
        }

        public void Read(SellerMail sellerMail)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (Buy.SellerResult) sellerMail.attachment;

            //TODO 관련 기획이 끝나면 별도 UI를 생성
            LocalLayerModifier.ModifyAgentGold(agentAddress, attachment.gold);
            LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, sellerMail.id);
        }

        public void Read(ItemEnhanceMail itemEnhanceMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (ItemEnhancement.ResultModel) itemEnhanceMail.attachment;
            var popup = Find<CombinationResultPopup>();
            var itemBase = attachment.itemUsable ?? (ItemBase)attachment.costume;
            var nonFungibleItem = attachment.itemUsable ?? (INonFungibleItem)attachment.costume;
            var model = new UI.Model.CombinationResultPopup(new CountableItem(itemBase, 1))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, nonFungibleItem.ItemId, false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, itemEnhanceMail.id);
            });
            popup.Pop(model);
        }

        public void Read(DailyRewardMail dailyRewardMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (DailyReward.DailyRewardResult) dailyRewardMail.attachment;
            var popup = Find<DailyRewardItemPopup>();
            var materials = attachment.materials;
            var material = materials.First();

            var model = new UI.Model.ItemCountConfirmPopup();
            model.TitleText.Value = L10nManager.Localize("UI_DAILY_REWARD_POPUP_TITLE");
            model.Item.Value = new CountEditableItem(material.Key, material.Value, material.Value, material.Value);
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalLayerModifier.AddItem(avatarAddress, material.Key.ItemId, material.Value, false);
                LocalLayerModifier.RemoveNewAttachmentMail(avatarAddress, dailyRewardMail.id);
                popup.Close();
            }).AddTo(gameObject);
            popup.Pop(model);
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
    }
}
