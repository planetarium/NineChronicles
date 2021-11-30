using System;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.L10n;

namespace Nekoyume.UI.Module
{
    using Nekoyume.UI.Scroller;
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
        private GameObject normalObject = null;

        [SerializeField]
        private GameObject conditionalObject = null;

        [SerializeField]
        private GameObject disabledObject = null;

        [SerializeField]
        private string conditionInfoKey = null;

        public System.Action<State> OnClick { protected get; set; }

        protected readonly ReactiveProperty<State> CurrentState = new ReactiveProperty<State>();

        private Button _button = null;
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
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClickButton);
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
            if (_interactable)
            {
                var condition = CheckCondition();

                if (condition)
                {
                    normalObject.SetActive(true);
                    conditionalObject.SetActive(false);
                    CurrentState.Value = State.Normal;
                }
                else
                {
                    normalObject.SetActive(false);
                    conditionalObject.SetActive(true);
                    CurrentState.Value = State.Conditional;
                }
            }
            else
            {
                normalObject.SetActive(false);
                conditionalObject.SetActive(false);
                CurrentState.Value = State.Disabled;
            }
            disabledObject.SetActive(!_interactable);
        }

        protected virtual void OnClickButton()
        {
            OnClick?.Invoke(CurrentState.Value);

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
