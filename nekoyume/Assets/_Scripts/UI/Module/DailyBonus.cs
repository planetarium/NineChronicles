using System;
using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class DailyBonus : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Slider slider = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private CanvasGroup additiveCanvasGroup = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        [SerializeField]
        private Transform boxImageTransform = null;

        [SerializeField, CanBeNull]
        private ActionPoint actionPoint = null;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _updateEnable;
        private bool _isFull;
        private Animator _animator;
        private long _receivedIndex;

        private VanilaTooltip _tooltip;
        private Coroutine _lerpCoroutine;

        private static readonly int IsFull = Animator.StringToHash("IsFull");
        private static readonly int Reward = Animator.StringToHash("GetReward");

        #region Mono

        private void Awake()
        {
            slider.maxValue = GameConfig.DailyRewardInterval;
            text.text = $"0 / {GameConfig.DailyRewardInterval}";
            slider.value = 0;
            button.interactable = false;
            _animator = GetComponent<Animator>();
            _updateEnable = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            additiveCanvasGroup.alpha = 0f;
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(SetIndex).AddTo(_disposables);
            ReactiveAvatarState.DailyRewardReceivedIndex.Subscribe(SetReceivedIndex).AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        #endregion

        public void SetIndex(int index) => SetIndex((long) index);

        private void SetIndex(long index)
        {
            if (!_updateEnable)
            {
                return;
            }

            var min = Math.Max(index - _receivedIndex, 0);
            var value = Math.Min(min, GameConfig.DailyRewardInterval);
            _isFull = value >= GameConfig.DailyRewardInterval;

            if (_lerpCoroutine != null)
                StopCoroutine(_lerpCoroutine);

            _lerpCoroutine = StartCoroutine(LerpSlider((int) value));
        }

        private IEnumerator LerpSlider(int value, int additionalSpeed = 1)
        {
            var current = slider.value;
            var speed = 4 * additionalSpeed;

            while (current <= value - 2)
            {
                current = Mathf.Lerp(current, value, Time.deltaTime * speed);
                slider.value = current;
                text.text = $"{(int) current} / {GameConfig.DailyRewardInterval}";
                yield return null;
            }

            slider.value = value;
            text.text = $"{value} / {GameConfig.DailyRewardInterval}";
            additiveCanvasGroup.alpha = _isFull ? 1 : 0;
            additiveCanvasGroup.interactable = _isFull;
            button.interactable = _isFull;
            _animator.SetBool(IsFull, _isFull);
        }

        public void GetReward()
        {
            Game.Game.instance.ActionManager.DailyReward().Subscribe(_ =>
            {
                _updateEnable = true;
                Notification.Push(Nekoyume.Model.Mail.MailType.System,
                    LocalizationManager.Localize("UI_RECEIVED_DAILY_REWARD"));
            });
            Notification.Push(Nekoyume.Model.Mail.MailType.System,
                LocalizationManager.Localize("UI_RECEIVING_DAILY_REWARD"));
            additiveCanvasGroup.alpha = 0;
            additiveCanvasGroup.interactable = false;
            button.interactable = false;
            _isFull = false;
            _animator.SetBool(IsFull, _isFull);
            _animator.StopPlayback();
            _animator.SetTrigger(Reward);
            VFXController.instance.Create<ItemMoveVFX>(boxImageTransform.position);

            if (!(actionPoint is null))
            {
                ItemMoveAnimation.Show(actionPoint.Image.sprite,
                    boxImageTransform.position,
                    actionPoint.Image.transform.position,
                    true,
                    1f,
                    0.8f);                
            }
            
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
