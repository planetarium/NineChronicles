using System;
using System.Collections.Generic;
using Libplanet.Assets;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume
{
    using UniRx;

    public class ItemTooltipBuy : MonoBehaviour
    {
        [SerializeField]
        private BlockTimer timer;

        [SerializeField]
        private SubmitWithCostButton button;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private long _expiredBlockIndex;

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(SetBlockIndex)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void SetBlockIndex(long blockIndex)
        {
            var value = _expiredBlockIndex - blockIndex;
            button.SetSubmittable(value > 0);
        }

        public void Set(long expiredBlockIndex, FungibleAssetValue ncg, bool isEnough,
            System.Action onSubmit)
        {
            var value = expiredBlockIndex - Game.Game.instance.Agent.BlockIndex;
            button.SetSubmittable(value > 0);

            button.OnSubmitClick.Dispose();
            button.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onSubmit?.Invoke();
            }).AddTo(gameObject);
            timer.UpdateTimer(expiredBlockIndex);
        }
    }
}
