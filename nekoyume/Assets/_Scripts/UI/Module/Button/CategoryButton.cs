using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.AnimatedGraphics;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CategoryButton : MonoBehaviour, IToggleable
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Image normalImage = null;

        [SerializeField]
        private Image selectedImage = null;

        [SerializeField]
        private TextMeshProUGUI normalText = null;

        [SerializeField]
        private TextMeshProUGUI selectedText = null;

        [SerializeField]
        private TextMeshProUGUI disabledText = null;

        [SerializeField]
        private Image hasNotificationImage = null;

        [SerializeField]
        private string localizationKey = null;

        private IToggleListener _toggleListener;

        public readonly Subject<CategoryButton> OnClick = new Subject<CategoryButton>();
        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

        private void Awake()
        {
            Toggleable = true;

            if (!string.IsNullOrEmpty(localizationKey))
            {
                var localization = L10nManager.Localize(localizationKey);
                normalText.text = localization;
                selectedText.text = localization;
            }

            button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);
            HasNotification.SubscribeTo(hasNotificationImage).AddTo(gameObject);

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

        public bool IsToggledOn => selectedImage.enabled;

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

            selectedImage.enabled = true;
            normalText.enabled = false;
            selectedText.enabled = true;
            disabledText.enabled = false;
        }

        public void SetToggledOff()
        {
            if (!Toggleable)
            {
                return;
            }

            selectedImage.enabled = false;
            normalText.enabled = true;
            selectedText.enabled = false;
            disabledText.enabled = false;
        }

        #endregion

        public void SetInteractable(bool interactable, bool ignoreImageColor = false)
        {
            button.interactable = interactable;

            if (ignoreImageColor)
            {
                return;
            }

            var imageColor = button.interactable
                ? Color.white
                : Color.gray;
            normalImage.color = imageColor;
            selectedImage.color = imageColor;
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
