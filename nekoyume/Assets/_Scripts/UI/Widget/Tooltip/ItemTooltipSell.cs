using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
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

        public void Set(long expiredBlockIndex, System.Action onRetrieve, System.Action onRegister)
        {
            _disposables.DisposeAllAndClear();

            retrieveButton.SetCost(CostType.ActionPoint, 5);
            retrieveButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onRetrieve?.Invoke();
            }).AddTo(_disposables);
            registerButton.SetCost(CostType.ActionPoint, 5);
            registerButton.OnSubmitSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onRegister?.Invoke();
            }).AddTo(_disposables);

            timer.UpdateTimer(expiredBlockIndex);
        }
    }
}
