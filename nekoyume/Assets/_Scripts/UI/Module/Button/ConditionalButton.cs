using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Nekoyume.L10n;
    using UniRx;

    public class ConditionalButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject normalObject = null;

        [SerializeField]
        private GameObject conditionalObject = null;

        [SerializeField]
        private GameObject disabledObject = null;

        [SerializeField]
        private string conditionInfoKey = null;

        public System.Action OnClick { protected get; set; }

        private Button _activatedButton = null;
        private Func<bool> _conditionFunc = null;
        private IDisposable _onClickDisposable = null;
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

        private void OnEnable()
        {
            UpdateObjects();
        }

        private void OnDisable()
        {
            _onClickDisposable?.Dispose();
        }

        private void OnDestroy()
        {
            _onClickDisposable?.Dispose();
        }

        public void SetCondition(Func<bool> conditionFunc)
        {
            _conditionFunc = conditionFunc;
            UpdateObjects();
        }

        public void UpdateObjects()
        {
            if (_interactable)
            {
                var condition = _conditionFunc?.Invoke() ?? false;

                if (condition)
                {
                    normalObject.SetActive(true);
                    conditionalObject.SetActive(false);
                    _activatedButton = normalObject.GetComponent<Button>();
                    _onClickDisposable?.Dispose();
                    _onClickDisposable = _activatedButton
                        .OnClickAsObservable()
                        .Subscribe(_ => OnClick?.Invoke());
                }
                else
                {
                    normalObject.SetActive(false);
                    conditionalObject.SetActive(true);
                    _activatedButton = conditionalObject.GetComponent<Button>();
                    _onClickDisposable?.Dispose();
                    if (!string.IsNullOrEmpty(conditionInfoKey))
                    {
                        _onClickDisposable = _activatedButton
                            .OnClickAsObservable()
                            .Subscribe(_ =>
                                NotificationSystem.Push(Nekoyume.Model.Mail.MailType.System, L10nManager.Localize(conditionInfoKey)));
                    }
                }
            }
            else
            {
                normalObject.SetActive(false);
                conditionalObject.SetActive(false);
                _activatedButton = disabledObject.GetComponent<Button>();
                _onClickDisposable?.Dispose();
                _onClickDisposable = _activatedButton
                    .OnClickAsObservable()
                    .Subscribe();
            }
            disabledObject.SetActive(!_interactable);
        }
    }
}
