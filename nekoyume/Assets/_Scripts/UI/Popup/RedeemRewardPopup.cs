using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI
{
    public class RedeemRewardPopup : PopupWidget
    {
        public SimpleCountableItemView[] itemViews;
        public SubmitButton submitButton;
        public TouchHandler touchHandler;

        public override void Initialize()
        {
            base.Initialize();

            submitButton.SetSubmitText(
                LocalizationManager.Localize("UI_CLOSE"),
                LocalizationManager.Localize("UI_CLOSE")
            );

            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Close();
            }).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                {
                    AudioController.PlayClick();
                    Close();
                }
            }).AddTo(gameObject);

            CloseWidget = null;
        }

        public void Pop(List<(ItemBase, int)> items, TableSheets tableSheets)
        {
            for (var i = 0; i < itemViews.Length; i++)
            {
                var view = itemViews[i];
                view.gameObject.SetActive(false);
                if (i < items.Count)
                {
                    var (item, count) = items[i];
                    var countableItem = new CountableItem(item, count);
                    view.SetData(countableItem);
                    view.gameObject.SetActive(true);
                }
            }

            base.Show();
        }
    }
}
