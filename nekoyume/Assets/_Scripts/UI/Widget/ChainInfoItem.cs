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
        private const string DefaultUrl = "https://ninechronicles.medium.com/";
        
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
            UpdateUI();
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
        
        private void UpdateBlockIndex(long _)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var thorSchedule = Nekoyume.Game.LiveAsset.LiveAssetManager.instance.ThorSchedule;
            if (thorSchedule is null || !thorSchedule.IsOpened)
            {
                return;
            }
            
            var remainBlock = thorSchedule.DiffFromEndBlockIndex;
            BlockIndexText.text = $"Remaining Time <style=G5>{remainBlock:#,0}({remainBlock.BlockRangeToTimeSpanString()})";
        }
        
        private void OpenDetailWebPage()
        {
            var thorSchedule = Nekoyume.Game.LiveAsset.LiveAssetManager.instance.ThorSchedule;
            Application.OpenURL(thorSchedule is null ? DefaultUrl : thorSchedule.InformationUrl);
            _onOpenDetailWebPage?.Invoke();
        }
    }
}
