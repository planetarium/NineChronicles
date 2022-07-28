using System;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.L10n;
using Nekoyume.UI.Scroller;
using TMPro;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ConditionalButton : MonoBehaviour
    {
        public enum State
        {
            Normal,
            Conditional,
            Disabled
        }

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Button disabledButton = null;

        [SerializeField]
        private GameObject normalObject = null;

        [SerializeField]
        private TextMeshProUGUI normalText = null;

        [SerializeField]
        private GameObject conditionalObject = null;

        [SerializeField]
        private TextMeshProUGUI conditionalText = null;

        [SerializeField]
        private GameObject disabledObject = null;

        [SerializeField]
        private TextMeshProUGUI disabledText = null;

        [SerializeField]
        private GameObject effectOverlay = null;

        [SerializeField]
        private string conditionInfoKey = null;

        public readonly Subject<State> OnClickSubject = new Subject<State>();

        public readonly Subject<Unit> OnSubmitSubject = new Subject<Unit>();

        private IObservable<Unit> _onClickDisabledSubject = null;

        public IObservable<Unit> OnClickDisabledSubject
        {
            get
            {
                if (_onClickDisabledSubject is null)
                {
                    _onClickDisabledSubject = disabledButton.OnClickAsObservable();
                }

                return _onClickDisabledSubject;
            }
        }

        public bool IsSubmittable => _interactable && CurrentState.Value == State.Normal;

        public string Text
        {
            get
            {
                if (!CurrentState.HasValue)
                {
                    return normalText.text;
                }

                return CurrentState.Value switch
                {
                    State.Normal => normalText.text,
                    State.Conditional => conditionalText.text,
                    State.Disabled => disabledText.text,
                    _ => normalText.text,
                };
            }
            set
            {
                normalText.text = value;
                conditionalText.text = value;
                disabledText.text = value;
            }
        }

        public readonly ReactiveProperty<State> CurrentState = new ReactiveProperty<State>();

        private Func<bool> _conditionFunc = null;
        private bool _interactable = true;

        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                UpdateObjects();
            }
        }

        protected virtual void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        protected virtual bool CheckCondition()
        {
            return _conditionFunc?.Invoke() ?? true;
        }

        public void SetCondition(Func<bool> conditionFunc)
        {
            _conditionFunc = conditionFunc;
        }

        public void SetText(State state, string text)
        {
            switch (state)
            {
                case State.Normal:
                    normalText.text = text;
                    break;
                case State.Conditional:
                    conditionalText.text = text;
                    break;
                case State.Disabled:
                    disabledText.text = text;
                    break;
            }
        }

        public virtual void UpdateObjects()
        {
            var condition = CheckCondition();
            SetConditionalState(condition);
            disabledObject.SetActive(!_interactable);
        }

        public void SetConditionalState(bool value)
        {
            if (_interactable)
            {
                SetState(value ? State.Normal : State.Conditional);
            }
            else
            {
                SetState(State.Disabled);
            }
        }

        public void SetState(State state)
        {
            CurrentState.Value = state;
            switch (state)
            {
                case State.Normal:
                    normalObject.SetActive(true);
                    conditionalObject.SetActive(false);
                    button.interactable = true;
                    break;
                case State.Conditional:
                    normalObject.SetActive(false);
                    conditionalObject.SetActive(true);
                    button.interactable = true;
                    break;
                case State.Disabled:
                    normalObject.SetActive(false);
                    conditionalObject.SetActive(false);
                    effectOverlay.SetActive(false);
                    button.interactable = false;
                    break;
            }
        }

        protected virtual void OnClickButton()
        {
            OnClickSubject.OnNext(CurrentState.Value);
            if (IsSubmittable)
            {
                AudioController.PlayClick();
                OnSubmitSubject.OnNext(default);
            }

            switch (CurrentState.Value)
            {
                case State.Normal:
                    UpdateObjects();
                    break;
                case State.Conditional:
                    if (!string.IsNullOrEmpty(conditionInfoKey))
                        NotificationSystem.Push(
                            Nekoyume.Model.Mail.MailType.System,
                            L10nManager.Localize(conditionInfoKey),
                            NotificationCell.NotificationType.Information);
                    break;
                case State.Disabled:
                    break;
            }
        }
    }
}
