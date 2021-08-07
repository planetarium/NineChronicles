using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Hourglass : AlphaAnimateModule
    {
        [SerializeField] 
        private TextMeshProUGUI countText = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        protected override void OnEnable()
        {
            base.OnEnable();
            ReactiveAvatarState.Inventory?.Subscribe(UpdateHourglass).AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposables.DisposeAllAndClear();
        }
      
        private void UpdateHourglass(Nekoyume.Model.Item.Inventory inventory)
        {
            var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
            Debug.Log($"[UpdateHourglass] : {count}");
            countText.text = count.ToString();
        }
    }
}
