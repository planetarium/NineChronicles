using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using Game;
    using UniRx;
    
    public class ChainInfoItem : MonoBehaviour
    {
        [field: SerializeField] public TMP_Text BlockIndexText { get; private set; }
        [field: SerializeField] public UnityEngine.UI.Button ViewDetailButton { get; private set; }
        
        private readonly List<IDisposable> _disposables = new();
        
        private System.Action _onOpenDetailWebPage;
        
        public event System.Action OnOpenDetailWebPage
        {
            add
            {
                _onOpenDetailWebPage -= value;
                _onOpenDetailWebPage += value;
            }
            remove => _onOpenDetailWebPage -= value;
        }
        
#region MonoBehaviour
        private void Awake()
        {
            ViewDetailButton.onClick.AddListener(OpenDetailWebPage);
        }

        private void OnEnable()
        {
            Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateBlockIndex)
                .AddTo(_disposables);
        }
        
        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
#endregion MonoBehaviour
        
        private void UpdateBlockIndex(long currentBlockIndex)
        {
            // BlockIndexText.text = $"{remainBlock:#,0}({remainBlock.BlockRangeToTimeSpanString()})";
        }
        
        public void OpenDetailWebPage()
        {
            // TODO: dynamic url
            var detailUrl = "https://dotnet.microsoft.com/download";
            Application.OpenURL(detailUrl);
            _onOpenDetailWebPage?.Invoke();
        }
    }
}
