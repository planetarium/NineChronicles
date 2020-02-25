using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class DailyBonus : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Slider slider;
        public Button button;
        public CanvasGroup canvasGroup;
        public CanvasGroup dailyBonusCanvasGroup;
        public bool animateAlpha;
        public RectTransform tooltipArea;
        public Transform boxImageTransform;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _updateEnable;
        private bool _isFull;
        private Animator _animator;
        private long _receivedIndex;

        private VanilaTooltip _tooltip;
        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int Reward = Animator.StringToHash("GetReward");

        #region Mono

        private void Awake()
        {
            slider.maxValue = GameConfig.DailyRewardInterval;
            text.text = $"0 / {GameConfig.DailyRewardInterval}";
            button.interactable = false;
            _animator = GetComponent<Animator>();
            _updateEnable = true;
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(SetIndex).AddTo(_disposables);
            ReactiveAvatarState.DailyRewardReceivedIndex.Subscribe(SetReceivedIndex).AddTo(_disposables);
        }

        private void OnEnable()
        {
            canvasGroup.alpha = 0;
            
            if (animateAlpha)
            {
                dailyBonusCanvasGroup.alpha = 0;
                dailyBonusCanvasGroup.DOFade(1, 1.0f);
            }
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        #endregion

        private void SetIndex(long index)
        {
            if(!_updateEnable)
            {
                return;
            }

            var min = Math.Max(index - _receivedIndex, 0);
            var value = Math.Min(min, GameConfig.DailyRewardInterval);
            _isFull = value >= GameConfig.DailyRewardInterval;
            
            button.interactable = _isFull;
            canvasGroup.interactable = _isFull;
            _animator.SetBool(IsFull, _isFull);

            text.text = $"{value} / {GameConfig.DailyRewardInterval}";
            slider.value = value;
        }

        public void GetReward()
        {
            Game.Game.instance.ActionManager.DailyReward().Subscribe(_ =>
            {
                _updateEnable = true;
                Notification.Push(Nekoyume.Model.Mail.MailType.System, LocalizationManager.Localize("UI_RECEIVED_DAILY_REWARD"));
            });
            Notification.Push(Nekoyume.Model.Mail.MailType.System, LocalizationManager.Localize("UI_RECEIVING_DAILY_REWARD"));
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            button.interactable = false;
            _isFull = false;
            _animator.SetBool(IsFull, _isFull);
            _animator.StopPlayback();
            _animator.SetTrigger(Reward);
            VFXController.instance.Create<ItemMoveVFX>(boxImageTransform.position);
            SetIndex(0);
            _updateEnable = false;
        }

        public void ShowTooltip()
        {
            _tooltip = Widget.Find<VanilaTooltip>();
            _tooltip?.Show("UI_PROSPERITY_DEGREE", "UI_PROSPERITY_DEGREE_DESCRIPTION", tooltipArea.position);
        }

        public void HideTooltip()
        {
            _tooltip?.Close();
            _tooltip = null;
        }

        private void SetReceivedIndex(long index)
        {
            if (index != _receivedIndex)
            {
                _receivedIndex = index;
            }
        }
    }
}
