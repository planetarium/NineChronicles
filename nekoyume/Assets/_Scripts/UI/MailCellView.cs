using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MailCellView : EnhancedScrollerCellView
    {
        public Image icon;
        public Text label;
        public Game.Mail.Mail data;
        public Button button;
        public IObservable<Unit> onClickButton;
        public IDisposable onClickDisposable;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
            onClickButton = button.OnClickAsObservable();
        }

        private void OnDisable()
        {
            Clear();
        }

        #endregion

        public void SetData(Game.Mail.Mail mail)
        {
            data = mail;
            var text = mail.ToInfo();
            Sprite sprite;
            Color32 color;
            if (mail.New)
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_01");
                color = ColorHelper.HexToColorRGB("fff9dd");
                button.interactable = true;
            }
            else
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_02");
                color = ColorHelper.HexToColorRGB("7a7a7a");
                button.interactable = false;
            }
            icon.sprite = sprite;
            icon.SetNativeSize();
            label.text = text;
            label.color = color;
        }

        public void Read()
        {
            if (!data.New)
                return;

            data.New = false;
            button.interactable = false;
            label.color = ColorHelper.HexToColorRGB("7a7a7a");

            switch (data)
            {
                case CombinationMail _:
                {
                    var attachment = (Action.Combination.Result) data.attachment;
                    var item = attachment.itemUsable;
                    var popup = Widget.Find<CombinationResultPopup>();
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
                    break;
                }
                case SellCancelMail _:
                {
                    var attachment = (Action.SellCancellation.Result) data.attachment;
                    var item = attachment.itemUsable;
                    //TODO 관련 기획이 끝나면 별도 UI를 생성
                    var popup = Widget.Find<ItemCountAndPricePopup>();
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
                    break;
                }
            }
        }

        private void Clear()
        {
            onClickDisposable?.Dispose();
            button.interactable = true;
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
