using System;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.L10n;

namespace Nekoyume.UI.Module
{
    using System.Collections.Generic;
    using TMPro;
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
        private GameObject normalObject = null;

        [SerializeField]
        private GameObject conditionalObject = null;

        [SerializeField]
        private GameObject disabledObject = null;

        [SerializeField]
        private GameObject effectOverlay = null;

        [SerializeField]
        private string conditionInfoKey = null;

        [SerializeField]
        private List<TextMeshProUGUI> texts = null;

        public readonly Subject<State> OnClickSubject = new Subject<State>();

        public readonly Subject<Unit> OnSubmitSubject = new Subject<Unit>();

        public bool IsSubmittable => _interactable && CurrentState.Value == State.Normal;

        public string Text
        {
            get => texts[0].text;
            set
            {
                foreach (var text in texts)
                {
                    text.text = value;
                }
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

        public void UpdateObjects()
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
                    effectOverlay.SetActive(true);
                    button.interactable = true;
                    break;
                case State.Conditional:
                    normalObject.SetActive(false);
                    conditionalObject.SetActive(true);
                    effectOverlay.SetActive(false);
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
                OnSubmitSubject.OnNext(default);
            }

            switch (CurrentState.Value)
            {
                case State.Normal:
                    UpdateObjects();
                    break;
                case State.Conditional:
                    if (!string.IsNullOrEmpty(conditionInfoKey))
                        NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, L10nManager.Localize(conditionInfoKey));
                    break;
                case State.Disabled:
                    break;
            }
        }
    }
}
