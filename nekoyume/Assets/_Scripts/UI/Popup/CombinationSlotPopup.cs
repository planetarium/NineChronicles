using System.Globalization;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CombinationSlotPopup : PopupWidget
    {
        public EquipmentRecipeCellView equipmentCellView;
        public ConsumableRecipeCellView consumableCellView;
        public CombinationMaterialPanel materialPanel;
        public EquipmentOptionRecipeView optionView;
        public SubmitWithCostButton submitButton;
        public TouchHandler touchHandler;

        private int _slotIndex;
        private int _cost;
        private CombinationSelectSmallFrontVFX _frontVFX;
        private MaterialItemSheet.Row _row;

        protected override void Awake()
        {
            base.Awake();

            submitButton.SetSubmitText(
                LocalizationManager.Localize("UI_COMBINATION_WAITING"),
                LocalizationManager.Localize("UI_RAPID_COMBINATION")
            );

            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                RapidCombination();
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

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            if (_frontVFX)
            {
                _frontVFX.Stop();
            }
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            _frontVFX =
                VFXController.instance.Create<CombinationSelectSmallFrontVFX>(
                    equipmentCellView.transform,
                    new Vector3(0.53f, -0.5f));
        }

        public void Pop(CombinationSlotState state, int slotIndex)
        {
            _slotIndex = slotIndex;
            var result = (CombinationConsumable.ResultModel) state.Result;
            var subRecipeEnabled = result.subRecipeId.HasValue;
            materialPanel.gameObject.SetActive(false);
            optionView.gameObject.SetActive(false);
            switch (result.itemType)
            {
                case ItemType.Equipment:
                {
                    var recipeRow =
                        Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.Values.First(r =>
                            r.Id == result.recipeId);

                    equipmentCellView.Set(recipeRow);
                    if (subRecipeEnabled)
                    {
                        optionView.Show(
                            result.itemUsable.GetLocalizedName(),
                            (int) result.subRecipeId,
                            new EquipmentItemSubRecipeSheet.MaterialInfo(
                                recipeRow.MaterialId,
                                recipeRow.MaterialCount),
                            false
                        );
                    }
                    else
                    {
                        materialPanel.SetData(recipeRow, null, false);
                        materialPanel.gameObject.SetActive(true);
                    }

                    break;
                }
                case ItemType.Consumable:
                {
                    var recipeRow =
                        Game.Game.instance.TableSheets.ConsumableItemRecipeSheet.Values.First(r =>
                            r.Id == result.recipeId);

                    consumableCellView.Set(recipeRow);
                    materialPanel.SetData(recipeRow);
                    materialPanel.gameObject.SetActive(true);
                    break;
                }
            }

            submitButton.HideAP();
            submitButton.HideNCG();
            var diff = result.itemUsable.RequiredBlockIndex - Game.Game.instance.Agent.BlockIndex;
            if (diff < 0)
            {
                submitButton.SetSubmitText(
                    LocalizationManager.Localize("UI_COMBINATION_WAITING"),
                    LocalizationManager.Localize("UI_RAPID_COMBINATION")
                );
                submitButton.SetSubmittable(result.id != default);
                submitButton.HideHourglass();
            }
            else
            {
                _cost = Action.RapidCombination.CalculateHourglassCount(
                    States.Instance.GameConfigState,
                    diff);
                _row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.Hourglass);
                var isEnough =
                    States.Instance.CurrentAvatarState.inventory.HasItem(_row.ItemId, _cost);

                if (result.id != default)
                {
                    submitButton.SetSubmitText(
                        LocalizationManager.Localize("UI_RAPID_COMBINATION"));
                    submitButton.SetSubmittable(isEnough);
                }
                else
                {
                    submitButton.SetSubmitText(
                        LocalizationManager.Localize("UI_COMBINATION_WAITING"));
                    submitButton.SetSubmittable(false);
                }

                submitButton.ShowHourglass(_cost, isEnough);
            }

            base.Show();
        }

        private void RapidCombination()
        {
            LocalStateModifier.RemoveItem(States.Instance.CurrentAvatarState.address, _row.ItemId,
                _cost);
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            LocalStateModifier.UnlockCombinationSlot(_slotIndex, blockIndex);
            var slotState = States.Instance.CombinationSlotStates[_slotIndex];
            var result = (CombinationConsumable.ResultModel) slotState.Result;
            LocalStateModifier.AddNewResultAttachmentMail(
                States.Instance.CurrentAvatarState.address, result.id, blockIndex);
            var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            Notification.Push(
                MailType.Workshop,
                string.Format(CultureInfo.InvariantCulture, format,
                    result.itemUsable.GetLocalizedName())
            );
            Notification.Remove(result.itemUsable.ItemId);
            Game.Game.instance.ActionManager.RapidCombination(_slotIndex);
        }
    }
}
