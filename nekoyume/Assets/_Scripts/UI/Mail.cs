using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Mail : Widget, IMail
    {
        public enum MailTabState
        {
            All = 0,
            Forge,
            Auction,
            System
        }

        [Serializable]
        public class TabButton
        {
            private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("001870");
            private static readonly Vector2 _highlightedSize = new Vector2(143f, 60f);
            private static readonly Vector2 _unHighlightedSize = new Vector2(116f, 36f);
            public Sprite highlightedSprite;
            public Button button;
            public Image image;
            public Image icon;
            public Text text;
            public MailTabState state;
            private Shadow[] _textShadows;

            public void Init(MailTabState state)
            {
                if (!button) return;
                _textShadows = button.GetComponentsInChildren<Shadow>();
            }

            public void ChangeColor(bool isHighlighted = false)
            {
                image.overrideSprite = isHighlighted ? _selectedButtonSprite : null;
                // 금색 버튼 리소스로 변경 시 주석 해제
                // image.rectTransform.sizeDelta = isHighlighted ? _highlightedSize : _unHighlightedSize;
                icon.overrideSprite = isHighlighted ? highlightedSprite : null;
                foreach (var shadow in _textShadows)
                    shadow.effectColor = isHighlighted ? _highlightedColor : Color.black;
            }
        }
        
        public MailTabState tabState;
        public MailScrollerController scroller;
        public TabButton allButton;
        public TabButton forgeButton;
        public TabButton auctionButton;
        public TabButton systemButton;

        private static Sprite _selectedButtonSprite;
        private MailBox _mailBox;

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
            allButton.Init(MailTabState.All);
            forgeButton.Init(MailTabState.Forge);
            auctionButton.Init(MailTabState.Auction);
            systemButton.Init(MailTabState.System);
        }

        public override void Show()
        {
            tabState = MailTabState.All;
            _mailBox = States.Instance.currentAvatarState.Value.mailBox;
            ChangeState(0);
            base.Show();
        }

        public void ChangeState(int state)
        {
            tabState = (MailTabState) state;
            allButton.ChangeColor(tabState == MailTabState.All);
            forgeButton.ChangeColor(tabState == MailTabState.Forge);
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
            var attachment = (Action.Combination.Result) mail.attachment;
            var item = attachment.itemUsable;
            var popup = Find<CombinationResultPopup>();
            var materialItems = attachment.materials
                .Select(material => new {material, item = ItemFactory.CreateMaterial(material.id, Guid.Empty)})
                .Select(t => new CombinationMaterial(t.item, t.material.count, t.material.count, t.material.count))
                .ToList();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(item, 1))
            {
                isSuccess = true,
                materialItems = materialItems
            };
            popup.Pop(model);

            AddItem(item);
        }

        public void Read(SellCancelMail mail)
        {
            var attachment = (SellCancellation.Result) mail.attachment;
            var item = attachment.itemUsable;
            //TODO 관련 기획이 끝나면 별도 UI를 생성
            var popup = Find<ItemCountAndPricePopup>();
            var model = new UI.Model.ItemCountAndPricePopup();
            model.PriceInteractable.Value = true;
            model.Price.Value = attachment.shopItem.Price;
            model.CountEnabled.Value = false;
            model.Item.Value = new CountEditableItem(item, 1, 1, 1);
            model.OnClickSubmit.Subscribe(_ =>
            {
                AddItem(item);
                popup.Close();
            }).AddTo(gameObject);
            model.OnClickCancel.Subscribe(_ =>
            {
                AddItem(item);
                popup.Close();
            }).AddTo(gameObject);
            //TODO 재판매 처리추가되야함
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

            AddItem(item);
        }

        public void Read(SellerMail sellerMail)
        {
            var attachment = (Buy.SellerResult) sellerMail.attachment;
            //TODO 관련 기획이 끝나면 별도 UI를 생성
            AddGold(attachment.gold);
            Notification.Push($"{attachment.shopItem.Price:n0} 골드 획득");
        }

        public void Read(ItemEnhanceMail itemEnhanceMail)
        {
            var attachment = (ItemEnhancement.Result) itemEnhanceMail.attachment;
            var popup = Find<CombinationResultPopup>();
            var item = attachment.itemUsable;
            var model = new UI.Model.CombinationResultPopup(new CountableItem(item, 1))
            {
                isSuccess = true,
                materialItems = new List<CombinationMaterial>()
            };
            popup.Pop(model);

            AddItem(item);
        }

        private static void AddItem(ItemUsable item)
        {
            //아바타상태 인벤토리 업데이트
            ActionManager.instance.AddItem(item.ItemId);

            //게임상의 인벤토리 업데이트
            States.Instance.CurrentAvatarState.Value.inventory.AddItem(item);
        }

        private static void AddGold(decimal gold)
        {
            //판매자 에이전트 골드 업데이트
            ActionManager.instance.AddGold();

            //게임상의 골드 업데이트
            States.Instance.AgentState.Value.gold += gold;
            ReactiveAgentState.Gold.SetValueAndForceNotify(States.Instance.AgentState.Value.gold);
        }

    }
}
