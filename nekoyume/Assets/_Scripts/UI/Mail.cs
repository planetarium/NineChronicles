using System;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Mail : Widget
    {
        public MailScrollerController scroller;

        public override void Show()
        {
            var mailBox = States.Instance.currentAvatarState.Value.mailBox;
            scroller.SetData(mailBox);
            base.Show();
        }

        public void GetAttachment(MailCellView info)
        {
            var item = info.data.attachment.itemUsable;
            var popup = Find<CombinationResultPopup>();
            var materialItems = info.data.attachment.materials
                .Select(material => new {material, item = ItemFactory.CreateMaterial(material.id, Guid.Empty)})
                .Select(t => new CombinationMaterial(t.item, t.material.count, t.material.count, t.material.count))
                .ToList();
            var model = new UI.Model.CombinationResultPopup(new CountableItem(item, 1))
            {
                isSuccess = true,
                materialItems = materialItems
            };
            popup.Pop(model);
            info.Read();

            //게임상의 인벤토리 업데이트
            var player = Game.Game.instance.stage.GetPlayer();
            player.Inventory.AddNonFungibleItem(item);

            //아바타상태 인벤토리 업데이트
            ActionManager.instance.AddItem(item.ItemId);
        }

    }
}
