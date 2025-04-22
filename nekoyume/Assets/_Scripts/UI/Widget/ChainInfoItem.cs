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
            Observable.Interval(TimeSpan.FromMinutes(1))
                .Subscribe(_ => UpdateUI())
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
#endregion MonoBehaviour

        private void UpdateUI()
        {
            var thorSchedule = Nekoyume.Game.LiveAsset.LiveAssetManager.instance.ThorSchedule;
            if (thorSchedule is null || !thorSchedule.IsOpened)
            {
                return;
            }

            var timeSpan = thorSchedule.DiffFromEndTimeSpan;
            BlockIndexText.text = $"{L10n.L10nManager.Localize("UI_REMAINING_TIME_ONLY")} <style=Clock>{timeSpan.TimespanToString()}";
        }

        private void OpenDetailWebPage()
        {
            var thorSchedule = Nekoyume.Game.LiveAsset.LiveAssetManager.instance.ThorSchedule;
            var targetUrl = thorSchedule is null ? DefaultUrl : thorSchedule.InformationUrl;

            NcDebug.Log($"On Open Detail Web Page: {targetUrl}, number of subscribers: {_onOpenDetailWebPage?.GetInvocationList().Length}");
            Helper.Util.OpenURL(targetUrl);
            _onOpenDetailWebPage?.Invoke();
        }
    }
}
