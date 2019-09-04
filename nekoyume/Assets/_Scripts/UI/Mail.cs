using System;
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
            model.priceInteractable.Value = true;
            model.price.Value = attachment.shopItem.price;
            model.countEnabled.Value = false;
            model.item.Value = new CountEditableItem(item, 1, 1, 1);
            model.onClickCancel.Subscribe(_ =>
            {
                AddItem(item);
                popup.Close();
            }).AddTo(gameObject);
            //TODO 재판매 처리추가되야함
            popup.Pop(model);
        }

        private static void AddItem(ItemUsable item)
        {
            //아바타상태 인벤토리 업데이트
            ActionManager.instance.AddItem(item.ItemId);

            //게임상의 인벤토리 업데이트
            var newState = (AvatarState) States.Instance.currentAvatarState.Value.Clone();
            newState.inventory.AddNonFungibleItem(item);
            var index = States.Instance.currentAvatarKey.Value;
            ActionRenderHandler.Instance.UpdateLocalAvatarState(newState, index);
        }
    }
}
