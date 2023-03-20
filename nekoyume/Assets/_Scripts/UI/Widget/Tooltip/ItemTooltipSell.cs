using System;
using System.Collections.Generic;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume
{
    using UniRx;

    public class ItemTooltipSell : MonoBehaviour
    {
        [SerializeField]
        private BlockTimer timer;

        [SerializeField]
        private ConditionalCostButton retrieveButton;

        [SerializeField]
        private ConditionalCostButton registerButton;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        // It's a function for legacy. You must delete it after 100380
        public void Set(
            long expiredBlockIndex,
            int apStoneCount,
            Action<ConditionalButton.State> onRetrieve,
            Action<ConditionalButton.State> onRegister)
        {
            _disposables.DisposeAllAndClear();

            retrieveButton.SetCost(CostType.ActionPoint, 5);
            retrieveButton.Interactable = apStoneCount > 0;
            retrieveButton.OnClickSubject.Subscribe(onRetrieve).AddTo(_disposables);

            registerButton.SetCost(CostType.ActionPoint, 5);
            registerButton.Interactable = apStoneCount > 0;
            registerButton.OnClickSubject.Subscribe(onRegister).AddTo(_disposables);

            timer.gameObject.SetActive(true);
            timer.UpdateTimer(expiredBlockIndex);
        }

        public void Set(
            int apStoneCount,
            Action<ConditionalButton.State> onRetrieve,
            Action<ConditionalButton.State> onRegister)
        {
            _disposables.DisposeAllAndClear();

            retrieveButton.SetCost(CostType.ActionPoint, 5);
            retrieveButton.Interactable = apStoneCount > 0;
            retrieveButton.OnClickSubject.Subscribe(onRetrieve).AddTo(_disposables);

            registerButton.SetCost(CostType.ActionPoint, 5);
            registerButton.Interactable = apStoneCount > 0;
            registerButton.OnClickSubject.Subscribe(onRegister).AddTo(_disposables);

            timer.gameObject.SetActive(false);
        }
    }
}
