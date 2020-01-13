using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Mail : Widget, IMail
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
            private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("a35400");
            private static readonly Vector2 _highlightedSize = new Vector2(139f, 58f);
            private static readonly Vector2 _unHighlightedSize = new Vector2(116f, 36f);
            private static readonly Vector2 _leftBottom = new Vector2(-15f, -10.5f);
            private static readonly Vector2 _minusRightTop = new Vector2(15f, 13f);

            public Sprite highlightedSprite;
            public Button button;
            public Image image;
            public Image icon;
            public TextMeshProUGUI text;
            public TextMeshProUGUI textSelected;

            public void Init(string localizationKey)
            {
                if (!button) return;
                var localized = LocalizationManager.Localize(localizationKey);
                var content = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(localized.ToLower());
                text.text = content;
                textSelected.text = content;
            }

            public void ChangeColor(bool isHighlighted = false)
            {
                image.overrideSprite = isHighlighted ? _selectedButtonSprite : null;
                image.rectTransform.offsetMin = isHighlighted ? _leftBottom : Vector2.zero;
                image.rectTransform.offsetMax = isHighlighted ? _minusRightTop : Vector2.zero;
                icon.overrideSprite = isHighlighted ? highlightedSprite : null;
                text.gameObject.SetActive(!isHighlighted);
                textSelected.gameObject.SetActive(isHighlighted);
            }
        }

        public static readonly Dictionary<MailType, Sprite> mailIcons = new Dictionary<MailType, Sprite>();

        public MailTabState tabState;
        public MailScrollerController scroller;
        public TabButton allButton;
        public TabButton workshopButton;
        public TabButton auctionButton;
        public TabButton systemButton;
        public Blur blur;

        private static Sprite _selectedButtonSprite;
        private MailBox _mailBox;

        #region override

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");

            var path = "UI/Textures/icon_mail_Auction";
            mailIcons.Add(MailType.Auction, Resources.Load<Sprite>(path));
            path = "UI/Textures/icon_mail_Workshop";
            mailIcons.Add(MailType.Workshop, Resources.Load<Sprite>(path));
            path = "UI/Textures/icon_mail_System";
            mailIcons.Add(MailType.System, Resources.Load<Sprite>(path));

            allButton.Init("ALL");
            workshopButton.Init("UI_COMBINATION");
            auctionButton.Init("UI_SHOP");
            systemButton.Init("SYSTEM");
        }

        public override void Show()
        {
            tabState = MailTabState.All;
            _mailBox = States.Instance.CurrentAvatarState.mailBox;
            ChangeState(0);
            base.Show();
            blur?.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        public void UpdateList()
        {
            _mailBox = States.Instance.CurrentAvatarState.mailBox;
            if (_mailBox is null)
                return;

            float pos = scroller.scroller.ScrollPosition;
            ChangeState((int) tabState);
            scroller.scroller.ScrollPosition = pos;
        }

        public void ChangeState(int state)
        {
            tabState = (MailTabState) state;
            allButton.ChangeColor(tabState == MailTabState.All);
            workshopButton.ChangeColor(tabState == MailTabState.Workshop);
            auctionButton.ChangeColor(tabState == MailTabState.Auction);
            systemButton.ChangeColor(tabState == MailTabState.System);

            var list = _mailBox.ToList();
            if (state > 0)
            {
                list = list.FindAll(mail => mail.MailType == (MailType) state);
            }

            scroller.SetData(list);
        }

        public void Read(CombinationMail mail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (Action.Combination.ResultModel) mail.attachment;
            var item = attachment.itemUsable;
            var popup = Find<CombinationResultPopup>();
            var materialItems = attachment.materials
                .Select(pair => new {pair, item = pair.Key})
                .Select(t => new CombinationMaterial(t.item, t.pair.Value, t.pair.Value, t.pair.Value))
                .ToList();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(item, 1))
            {
                isSuccess = true,
                materialItems = materialItems
            };
            popup.Pop(model);

            LocalStateModifier.AddItem(avatarAddress, item.ItemId);
            LocalStateModifier.RemoveNewAttachmentMail(avatarAddress, item.ItemId);
        }

        public void Read(SellCancelMail mail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (SellCancellation.Result) mail.attachment;
            var item = attachment.itemUsable;
            //TODO 관련 기획이 끝나면 별도 UI를 생성
            var popup = Find<ItemCountAndPricePopup>();
            var model = new UI.Model.ItemCountAndPricePopup();
            model.TitleText.Value = LocalizationManager.Localize("UI_RETRIEVE");
            model.InfoText.Value = LocalizationManager.Localize("UI_SELL_CANCEL_INFO");
            model.PriceInteractable.Value = false;
            model.Price.Value = attachment.shopItem.Price;
            model.CountEnabled.Value = false;
            model.Item.Value = new CountEditableItem(item, 1, 1, 1);
            model.OnClickSubmit.Subscribe(_ =>
            {
                LocalStateModifier.RemoveNewAttachmentMail(avatarAddress, item.ItemId);
                popup.Close();
            }).AddTo(gameObject);
            model.OnClickCancel.Subscribe(_ =>
            {
                //TODO 재판매 처리추가되야함
                LocalStateModifier.RemoveNewAttachmentMail(avatarAddress, item.ItemId);
                popup.Close();
            }).AddTo(gameObject);
            popup.Pop(model);
        }

        public void Read(BuyerMail buyerMail)
        {
            var attachment = (Buy.BuyerResult) buyerMail.attachment;
            var item = attachment.itemUsable;
            var popup = Find<CombinationResultPopup>();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(item, 1))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            popup.Pop(model);

            AddItem(item, false);
        }

        public void Read(SellerMail sellerMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (Buy.SellerResult) sellerMail.attachment;

            //TODO 관련 기획이 끝나면 별도 UI를 생성
            
            AddGold(attachment.gold);
        }

        public void Read(ItemEnhanceMail itemEnhanceMail)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var attachment = (ItemEnhancement.ResultModel) itemEnhanceMail.attachment;
            var popup = Find<CombinationResultPopup>();
            var item = attachment.itemUsable;
            var model = new UI.Model.CombinationResultPopup(new CountableItem(item, 1))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            popup.Pop(model);

            LocalStateModifier.AddItem(avatarAddress, item.ItemId);
            LocalStateModifier.RemoveNewAttachmentMail(avatarAddress, item.ItemId);
        }

        private static void AddItem(ItemUsable item, bool canceled)
        {
            //아바타상태 인벤토리 업데이트
            ActionManager.instance.AddItem(item.ItemId, canceled);

            //게임상의 인벤토리 업데이트
            States.Instance.CurrentAvatarState.inventory.AddItem(item);
        }

        private static void AddGold(decimal gold)
        {
            //판매자 에이전트 골드 업데이트
            ActionManager.instance.AddGold();

            //게임상의 골드 업데이트
            States.Instance.AgentState.gold += gold;
            ReactiveAgentState.Gold.SetValueAndForceNotify(States.Instance.AgentState.gold);
        }
    }
}
