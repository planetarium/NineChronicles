using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Game;
    using UniRx;
    
    public class CombinationSlotAllPopup : PopupWidget
    {
        [SerializeField]
        private ConditionalCostButton rapidCombinationButton;

        [SerializeField]
        private Button bgButton;
        
        [SerializeField]
        private List<SimpleItemView> itemViews;

        private List<CombinationSlotState> _slotStateList;
        
        private readonly List<IDisposable> _disposablesOfOnEnable = new();

        protected override void Awake()
        {
            base.Awake();

            rapidCombinationButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    var slotIndexList = _slotStateList.Select(s => s.Index).ToList();
                    Game.instance.ActionManager.RapidCombination(_slotStateList, slotIndexList).Subscribe();
                    Find<CombinationSlotsPopup>().OnSendRapidCombination(slotIndexList);
                    Close();
                })
                .AddTo(gameObject);

            bgButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
        }

        protected override void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        public void Show(List<CombinationSlotState> stateList, long currentBlockIndex)
        {
            _slotStateList = stateList;
            SetActiveItemViews(stateList.Count);
            UpdateButtonInformation(stateList, currentBlockIndex);
            UpdateItemInformation(stateList);
            base.Show();
        }

        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            UpdateButtonInformation(_slotStateList, currentBlockIndex);
        }
        
        private void SetActiveItemViews(int count)
        {
            for (var i = 0; i < itemViews.Count; i++)
            {
                itemViews[i].gameObject.SetActive(i < count);
            }
        }
        
        private void UpdateButtonInformation(List<CombinationSlotState> stateList, long currentBlockIndex)
        {
            var cost = CombinationSlotsPopup.GetWorkingSlotsOpenCost(stateList, currentBlockIndex);
            rapidCombinationButton.SetCost(CostType.Hourglass, cost);
        }

        private void UpdateItemInformation(IReadOnlyList<CombinationSlotState> states)
        {
            for (var i = 0; i < states.Count; i++)
            {
                var item = states[i].Result.itemUsable;
                itemViews[i].SetData(new Item(item));
            }
        }
    }
}
