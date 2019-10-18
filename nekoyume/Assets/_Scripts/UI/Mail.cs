using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UniRx;

namespace Nekoyume.UI
{
    public class Mail : Widget, IMail
    {
        public MailScrollerController scroller;

        public override void Show()
        {
            var mailBox = States.Instance.currentAvatarState.Value.mailBox;
            scroller.SetData(mailBox);
            base.Show();
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
            model.Price.Value = attachment.shopItem.price;
            model.CountEnabled.Value = false;
            model.Item.Value = new CountEditableItem(item, 1, 1, 1);
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
            Notification.Push($"{attachment.shopItem.price:n0} 골드 획득");
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
            var newState = (AvatarState) States.Instance.currentAvatarState.Value.Clone();
            newState.inventory.AddNonFungibleItem(item);
            var index = States.Instance.currentAvatarKey.Value;
            ActionRenderHandler.UpdateLocalAvatarState(newState, index);
        }

        private static void AddGold(decimal gold)
        {
            //판매자 에이전트 골드 업데이트
            ActionManager.instance.AddGold();

            //게임상의 골드 업데이트
            var newAgentState = (AgentState) States.Instance.agentState.Value.Clone();
            newAgentState.gold += gold;
            ActionRenderHandler.UpdateLocalAgentState(newAgentState);
            var newAvatarState = (AvatarState) States.Instance.currentAvatarState.Value.Clone();
            var index = States.Instance.currentAvatarKey.Value;
            ActionRenderHandler.UpdateLocalAvatarState(newAvatarState, index);
        }

    }
}
