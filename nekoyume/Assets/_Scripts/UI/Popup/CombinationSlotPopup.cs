using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationSlotPopup : PopupWidget
    {
        public TextMeshProUGUI itemNameText;
        public CombinationItemInformation itemInformation;
        public CombinationMaterialPanel materialPanel;
        public EquipmentOptionRecipeView optionView;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;
        public TouchHandler touchHandler;

        protected override void Awake()
        {
            base.Awake();
            submitButtonText.text = LocalizationManager.Localize("UI_OK");

            submitButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Close();
            }).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (!pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                    return;

                AudioController.PlayClick();
                Close();
            }).AddTo(gameObject);

            CloseWidget = null;
            SubmitWidget = submitButton.onClick.Invoke;
        }

        public void Pop(CombinationSlotState state)
        {
            var result = (CombinationConsumable.ResultModel) state.Result;
            var resultItem = new CountableItem(result.itemUsable, 1);
            itemInformation.SetData(new Model.ItemInformation(resultItem));
            var subRecipeEnabled = result.subRecipeId.HasValue;
            materialPanel.gameObject.SetActive(!subRecipeEnabled);
            optionView.gameObject.SetActive(subRecipeEnabled);
            var recipeRow =
                Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values.First(r =>
                    r.Id == result.recipeId);
            if (subRecipeEnabled)
            {
                optionView.Show(
                    result.itemUsable.GetLocalizedName(),
                    (int) result.subRecipeId,
                    new EquipmentItemSubRecipeSheet.MaterialInfo(recipeRow.MaterialId, recipeRow.MaterialCount)
                );
            }
            else
            {
                materialPanel.SetData(recipeRow, null);
            }
            itemInformation.statsArea.root.gameObject.SetActive(false);
            itemInformation.skillsArea.root.gameObject.SetActive(false);
            itemNameText.text = result.itemUsable.GetLocalizedName();

            base.Show();
        }
    }
}
