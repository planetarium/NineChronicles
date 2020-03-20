using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class CombinationSlotPopup : PopupWidget
    {
        public TextMeshProUGUI itemNameText;
        public CombinationItemInformation itemInformation;
        public CombinationMaterialPanel materialPanel;
        public EquipmentOptionRecipeView optionView;
        public SubmitWithCostButton submitButton;
        public TouchHandler touchHandler;

        private int _slotIndex;
        private decimal _cost;

        protected override void Awake()
        {
            base.Awake();

            submitButton.submitText.text =
                LocalizationManager.Localize("UI_RAPID_COMBINATION");
            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                RapidCombination();
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
        }

        public void Pop(CombinationSlotState state, int slotIndex)
        {
            _slotIndex = slotIndex;
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
            submitButton.HideAP();
            submitButton.SetSubmittable(result.id != default);
            var cost = result.itemUsable.RequiredBlockIndex - Game.Game.instance.Agent.BlockIndex;
            if (cost < 0)
            {
                submitButton.HideNCG();
            }
            else
            {
                _cost = Convert.ToDecimal(cost);
                submitButton.ShowNCG(_cost, States.Instance.AgentState.gold >= _cost);
            }

            base.Show();
        }

        private void RapidCombination()
        {
            // Game.Game.instance.ActionManager.RapidCombination(_slotIndex);
            LocalStateModifier.ModifyAgentGold(States.Instance.AgentState.address, -_cost);
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            LocalStateModifier.UnlockCombinationSlot(_slotIndex, blockIndex);
            var slotState = States.Instance.CombinationSlotStates[_slotIndex];
            var result = (CombinationConsumable.ResultModel) slotState.Result;
            LocalStateModifier.AddNewResultAttachmentMail(States.Instance.CurrentAvatarState.address, result.id, blockIndex);
            var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            Notification.Push(
                MailType.Workshop,
                string.Format(format, result.itemUsable.Data.GetLocalizedName())
            );
            Notification.Remove(result.itemUsable.ItemId);
        }
    }
}
