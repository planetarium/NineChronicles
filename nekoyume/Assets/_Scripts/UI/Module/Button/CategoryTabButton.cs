using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.AnimatedGraphics;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CategoryTabButton : MonoBehaviour, IToggleable
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private GameObject normalObject = null;

        [SerializeField]
        private GameObject selectedObject = null;

        [SerializeField]
        private Image hasNotificationImage = null;

        private IToggleListener _toggleListener;

        public readonly Subject<CategoryTabButton> OnClick = new Subject<CategoryTabButton>();
        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

        private void Awake()
        {
            Toggleable = true;

            button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);
            if (hasNotificationImage)
            {
                HasNotification.SubscribeTo(hasNotificationImage).AddTo(gameObject);
            }

            InitializeMessageCat();
        }


        #region ILockableWithMessageCat // 가칭. IMessageCatTarget.. 등.

        // NOTE: 아래 로직들은 반복되니 인터페이스로 묶어서 로직을 한 곳으로 모을 수 있어 보인다.
        private bool _isLock;
        private int _lockCondition;
        private int _lockVariable;
        private string _messageForCat;
        private MessageCat _cat;

        private void InitializeMessageCat()
        {
            var go = gameObject;
            go.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (!_isLock)
                    {
                        return;
                    }

                    if (_cat)
                    {
                        _cat.Hide();
                    }

                    _cat = Widget.Find<MessageCatTooltip>().Show(true, _messageForCat, gameObject);
                })
                .AddTo(go);

            go.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x =>
                {
                    if (!_isLock || _cat is null)
                    {
                        return;
                    }

                    _cat.Hide();
                    _cat = null;
                })
                .AddTo(go);
        }

        public void SetLockCondition(int condition)
        {
            _lockCondition = condition;
            _messageForCat = string.Format(L10nManager.Localize("UI_REQUIRE_CLEAR_STAGE"),
                _lockCondition);
            UpdateLock();
        }

        public void SetLockVariable(int variable)
        {
            _lockVariable = variable;
            UpdateLock();
        }

        private void UpdateLock()
        {
            if (_lockVariable < _lockCondition)
            {
                Lock();
            }
            else
            {
                Unlock();
            }
        }

        private void Lock()
        {
            _isLock = true;
            SetInteractable(false);
        }

        private void Unlock()
        {
            _isLock = false;
            SetInteractable(true);
        }

        #endregion


        #region IToggleable

        public string Name => name;

        public bool Toggleable { get; set; }

        public bool IsToggledOn => selectedObject.activeSelf;

        public void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public void SetToggledOn()
        {
            if (!Toggleable)
            {
                return;
            }

            normalObject.gameObject.SetActive(false);
            selectedObject.gameObject.SetActive(true);
        }

        public void SetToggledOff()
        {
            if (!Toggleable)
            {
                return;
            }

            normalObject.gameObject.SetActive(true);
            selectedObject.gameObject.SetActive(false);
        }

        #endregion

        public void SetInteractable(bool interactable, bool ignoreImageColor = false)
        {
            button.interactable = interactable;

            if (ignoreImageColor)
            {
                return;
            }
        }

        private void SubscribeOnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClick.OnNext(this);

            if (IsToggledOn)
            {
                return;
            }

            _toggleListener?.OnToggle(this);
        }
    }
}
