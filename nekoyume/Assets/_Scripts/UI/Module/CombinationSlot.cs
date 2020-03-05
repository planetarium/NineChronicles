using System;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class CombinationSlot : MonoBehaviour
    {
        public UnityEngine.UI.Slider progressBar;
        public SimpleItemView resultView;
        public TextMeshProUGUI unlockText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI lockText;
        public TextMeshProUGUI sliderText;

        private void Awake()
        {
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(SetIndex).AddTo(gameObject);
        }

        public void SetData(CombinationSlotState state, long index)
        {
            var unlock = States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(state.UnlockStage);
            lockText.gameObject.SetActive(!unlock);
            resultView.gameObject.SetActive(false);
            if (unlock)
            {
                var canUse = state.Validate(States.Instance.CurrentAvatarState, index);
                if (!(state.Result is null))
                {
                    canUse = canUse && state.Result.itemUsable.RequiredBlockIndex <= index;
                    resultView.SetData(new Item(state.Result.itemUsable));
                    resultView.gameObject.SetActive(!canUse);
                }
                unlockText.gameObject.SetActive(canUse);
                progressText.gameObject.SetActive(!canUse);
                progressBar.gameObject.SetActive(!canUse);
            }

            progressBar.maxValue = state.UnlockBlockIndex;
        }

        private void SetIndex(long index)
        {
            var value = Math.Min(index, progressBar.maxValue);
            progressBar.value = value;
            sliderText.text = $"({value} / {progressBar.maxValue})";
        }
    }
}
