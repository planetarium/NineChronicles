using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using Game;
    using UniRx;
    
    public class ChainInfoPopup : PopupWidget
    {
        [SerializeField] private TMP_Text _blockIndexText;
        [SerializeField] private UnityEngine.UI.Button _closeButton;
        
        private readonly List<IDisposable> _disposables = new();
        
        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);            
        }

        protected override void OnEnable()
        {
            Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateBlockIndex)
                .AddTo(_disposables);
        }
        
        protected override void OnDisable()
        {
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }
        
        private void UpdateBlockIndex(long currentBlockIndex)
        {
            var sheet = Game.instance.TableSheets.ThorScheduleSheet;
            var row = sheet.GetRowByBlockIndex(currentBlockIndex);
            
            var remainBlock = row.EndBlockIndex - currentBlockIndex;
            _blockIndexText.text = $"{remainBlock:#,0}({remainBlock.BlockRangeToTimeSpanString()})";
        }
    }
}
