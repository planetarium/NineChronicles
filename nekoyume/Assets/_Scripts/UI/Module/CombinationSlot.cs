using System;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationSlot : MonoBehaviour
    {
        public Slider progressBar;
        public SimpleItemView resultView;
        public TextMeshProUGUI unlockText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI lockText;
        public TextMeshProUGUI sliderText;
        public TouchHandler touchHandler;
        public Image lockImage;

        private CombinationSlotState _data;
        private int _slotIndex;

        private void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(UpdateProgressBar)
                .AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                AudioController.PlayClick();
                SelectSlot();
            }).AddTo(gameObject);
            unlockText.text = LocalizationManager.Localize("UI_COMBINATION_SLOT_AVAILABLE");
        }

        public void SetData(CombinationSlotState state, long blockIndex, int slotIndex)
        {
            lockText.text = string.Format(LocalizationManager.Localize("UI_UNLOCK_CONDITION_STAGE"),
                state.UnlockStage);
            _data = state;
            _slotIndex = slotIndex;
            var unlock = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(state.UnlockStage);
            lockText.gameObject.SetActive(!unlock);
            lockImage.gameObject.SetActive(!unlock);
            unlockText.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
            progressBar.gameObject.SetActive(false);
            if (unlock)
            {
                resultView.Clear();
                var canUse = state.Validate(States.Instance.CurrentAvatarState, blockIndex);
                if (!(state.Result is null))
                {
                    canUse = canUse && state.Result.itemUsable.RequiredBlockIndex <= blockIndex;
                    if (!canUse)
                    {
                        resultView.SetData(new Item(state.Result.itemUsable));
                    }
                    progressText.text =
                        string.Format(LocalizationManager.Localize("UI_COMBINATION_SLOT_CRAFT"),
                            state.Result.itemUsable.GetLocalizedNonColoredName());
                }
                unlockText.gameObject.SetActive(canUse);
                progressText.gameObject.SetActive(!canUse);
                progressBar.gameObject.SetActive(!canUse);
            }

            progressBar.maxValue = state.RequiredBlockIndex;
            progressBar.value = blockIndex - state.StartBlockIndex;
            sliderText.text = $"({progressBar.value} / {progressBar.maxValue})";
        }

        private void UpdateProgressBar(long index)
        {
            var value = Math.Min(index - _data?.StartBlockIndex ?? index, progressBar.maxValue);
            progressBar.value = value;
            sliderText.text = $"({progressBar.value} / {progressBar.maxValue})";
        }

        private void ShowPopup()
        {
            if (_data?.Result is null)
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
            if (_data.Validate(States.Instance.CurrentAvatarState,
                Game.Game.instance.Agent.BlockIndex))
            {
                Widget.Find<Menu>().CombinationClick(_slotIndex);
            }
            else
            {
                ShowPopup();
            }
        }
    }
}
