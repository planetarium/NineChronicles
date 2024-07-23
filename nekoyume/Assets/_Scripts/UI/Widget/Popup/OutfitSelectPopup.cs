using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class OutfitSelectPopup : PopupWidget
    {
        [SerializeField]
        private SimpleItemScroll outfitScroll;

        [SerializeField]
        private Button submitButton;

        private Item _selectedItem;

        public override void Initialize()
        {
            base.Initialize();
            outfitScroll.OnClick.Subscribe(item =>
            {
                if (_selectedItem != null)
                {
                    _selectedItem.Selected.Value = false;
                }

                _selectedItem = item;
                _selectedItem.Selected.Value = true;
            }).AddTo(gameObject);
            submitButton.onClick.AddListener(OnClickSubmitButton);
        }

        public void Show(ItemSubType subType, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            // TODO: 지금은 그냥 EquipmentItemRecipeSheet에서 해당하는 ItemSubType을 다 뿌리고있다.
            // 나중에 외형을 갖고있는 시트 데이터로 바꿔치기 해야한다.
            outfitScroll.UpdateData(TableSheets.Instance.EquipmentItemRecipeSheet
                .Select(pair => pair.Value)
                .Where(r => r.ItemSubType == subType)
                .Select(r => new Item(ItemFactory.CreateItem(r.GetResultEquipmentItemRow(),
                    new ActionRenderHandler.LocalRandom(0)))));
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _selectedItem = null;
            base.Close(ignoreCloseAnimation);
        }

        private void OnClickSubmitButton()
        {
            if (_selectedItem is null)
            {
                OneLineSystem.Push(MailType.System, "select outfit", NotificationCell.NotificationType.Information);
                return;
            }

            // TODO: 이걸 나중에 커스텀 제작 액션으로 바꿔야함. 지금은 동작 테스트를 위해 억지로 CombinationEquipment를 붙였다.
            var selectedRecipeRow = TableSheets.Instance.EquipmentItemRecipeSheet.First(row =>
                row.Value.ResultEquipmentId == _selectedItem.ItemBase.Value.Id).Value;
            ActionManager.Instance
                .CombinationEquipment(new SubRecipeView.RecipeInfo
                {
                    RecipeId = selectedRecipeRow.Id,
                    SubRecipeId = selectedRecipeRow.SubRecipeIds[0],
                    CostNCG = default,
                    CostCrystal = default,
                    CostAP = 0,
                    Materials = new Dictionary<int, int>(),
                    ReplacedMaterials = new Dictionary<int, int>(),
                }, 0, true, false, null)
                .Subscribe();
            Close();
        }
    }
}
