using System;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class CombinationSlot : MonoBehaviour
    {
        public Slider progressBar;
        public SimpleItemView resultView;
        public TextMeshProUGUI unlockText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI lockText;
        public TextMeshProUGUI sliderText;
        public TouchHandler touchHandler;
        public Image hasNotificationImage;
        public Image lockImage;

        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>();

        private CombinationSlotState _data;
        private int _slotIndex;

        private long _blockIndex;

        private void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(gameObject);
            touchHandler.OnClick
                .Subscribe(pointerEventData =>
                {
                    AudioController.PlayClick();
                    SelectSlot();
                }).AddTo(gameObject);
            resultView.OnClick
                .Subscribe(pointerEventData =>
                {
                    AudioController.PlayClick();
                    SelectSlot();
                }).AddTo(gameObject);
            unlockText.text = L10nManager.Localize("UI_COMBINATION_SLOT_AVAILABLE");
            HasNotification.SubscribeTo(hasNotificationImage).AddTo(gameObject);
        }

        public void SetData(CombinationSlotState state, long blockIndex, int slotIndex)
        {
            lockText.text = string.Format(
                L10nManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                state.UnlockStage);
            _data = state;
            _slotIndex = slotIndex;
            var unlock = States.Instance.CurrentAvatarState?.worldInformation?
                .IsStageCleared(state.UnlockStage) ?? false;
            lockText.gameObject.SetActive(!unlock);
            lockImage.gameObject.SetActive(!unlock);
            unlockText.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
            progressBar.gameObject.SetActive(false);
            if (unlock)
            {
                var canUse = state.Validate(States.Instance.CurrentAvatarState, blockIndex);
                if (state.Result is null)
                {
                    resultView.Clear();
                }
                else
                {
                    canUse = canUse && state.Result.itemUsable.RequiredBlockIndex <= blockIndex;
                    if (canUse)
                    {
                        resultView.Clear();
                    }
                    else
                    {
                        resultView.SetData(new Item(state.Result.itemUsable));
                    }

                    progressText.text = state.Result.itemUsable.GetLocalizedName();
                }

                unlockText.gameObject.SetActive(canUse);
                progressText.gameObject.SetActive(!canUse);
                progressBar.gameObject.SetActive(!canUse);
                SubscribeOnBlockIndex(blockIndex);
                Widget.Find<BottomMenu>()?.UpdateCombinationNotification();
            }

            progressBar.maxValue = state.RequiredBlockIndex;
            progressBar.value = blockIndex - state.StartBlockIndex;
            sliderText.text = $"({progressBar.value} / {progressBar.maxValue})";
        }

        private void SubscribeOnBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateProgressBar(_blockIndex);
            UpdateHasNotification(_blockIndex);
        }

        private void UpdateHasNotification(long blockIndex)
        {
            if (_data is null || _data.Result is null)
            {
                HasNotification.Value = false;
                return;
            }

            switch (_data.Result)
            {
                case CombinationConsumable5.ResultModel ccResult:
                    if (ccResult.id == default)
                    {
                        HasNotification.Value = false;
                        return;
                    }
                    break;
                case ItemEnhancement.ResultModel _:
                default:
                    HasNotification.Value = false;
                    return;
            }

            var diff = _data.Result.itemUsable.RequiredBlockIndex - blockIndex;
            if (diff <= 0)
            {
                HasNotification.Value = false;
                return;
            }

            var gameConfigState = Game.Game.instance.States.GameConfigState;
            var cost = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);

            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.Hourglass);
            var isEnough =
                States.Instance.CurrentAvatarState.inventory.HasFungibleItem(row.ItemId, cost);

            HasNotification.Value = isEnough;
        }

        private void UpdateProgressBar(long index)
        {
            var value = Math.Min(index - _data?.StartBlockIndex ?? index, progressBar.maxValue);
            progressBar.value = value;
            sliderText.text = $"({progressBar.value} / {progressBar.maxValue})";
        }

        private void ShowPopup()
        {
            if (!(_data?.Result is CombinationConsumable5.ResultModel))
            {
                return;
            }

            if (_data.Result.itemUsable.RequiredBlockIndex > Game.Game.instance.Agent.BlockIndex)
            {
                Widget.Find<CombinationSlotPopup>().Pop(_data, _slotIndex);
            }
        }

        private void SelectSlot()
        {
            if (_data.Validate(
                States.Instance.CurrentAvatarState,
                Game.Game.instance.Agent.BlockIndex))
            {
                if (Game.Game.instance.Stage.IsInStage)
                {
                    return;
                }

                Widget.Find<Menu>().CombinationClick(_slotIndex);
            }
            else
            {
                ShowPopup();
            }
        }
    }
}
