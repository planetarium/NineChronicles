using Nekoyume.Game.Controller;
using System;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    [RequireComponent(typeof(Animator))]
    public class ToggleableButton : MonoBehaviour, IToggleable, IWidgetControllable
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private TextMeshProUGUI toggledOffText = null;

        [SerializeField]
        private Image toggledOffImage = null;

        [SerializeField]
        protected TextMeshProUGUI toggledOnText = null;

        [SerializeField]
        private Image toggledOnImage = null;

        [SerializeField]
        private Canvas sortingGroup = null;

        public string localizationKey = null;

        private IToggleListener _toggleListener;

        private Animator _animatorCache;
        private Color _originalTextColor;
        private int _originalSortingOrderOffset;

        public Animator Animator => !_animatorCache
            ? _animatorCache = GetComponent<Animator>()
            : _animatorCache;

        public readonly Subject<ToggleableButton> OnClick = new Subject<ToggleableButton>();
        public IObservable<PointerEventData> onPointerEnter = null;
        public IObservable<PointerEventData> onPointerExit = null;

        #region Mono

        protected virtual void Awake()
        {
            if (toggledOffText)
            {
                _originalTextColor = toggledOffText.color;
            }

            if (sortingGroup)
            {
                var widget = GetComponentInParent<Widget>();
                if (widget is HeaderMenuStatic)
                {
                    _originalSortingOrderOffset = 0;
                    sortingGroup.sortingOrder =
                        MainCanvas.instance.GetLayer(widget.WidgetType).root.sortingOrder;
                }
                else if (widget)
                {
                    var layerSortingOrder =
                        MainCanvas.instance.GetLayer(widget.WidgetType).root.sortingOrder;
                    _originalSortingOrderOffset = sortingGroup.sortingOrder - layerSortingOrder;
                }
                else
                {
                    _originalSortingOrderOffset = sortingGroup.sortingOrder;
                }
            }

            Toggleable = true;
            IsWidgetControllable = true;

            button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);
            onPointerEnter = gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable();
            onPointerExit = gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable();

            if (!string.IsNullOrEmpty(localizationKey))
            {
                SetText(L10nManager.Localize(localizationKey));
            }

            // (object) sortingGroup == (Canvas) "null" 이기 때문에 `is`나 `ReferenceEquals`를 사용하지 않습니다.
            // `SerializedField`는 `null`을 할당해도 객체 생성시 `"null"`이 되어버립니다.
            if (sortingGroup)
            {
                sortingGroup.sortingLayerName = "UI";
            }
        }

        #endregion

        #region IWidgetControllable

        private Widget _widget;
        private IDisposable _disposableForWidgetControllable;

        public bool IsWidgetControllable { get; set; }
        public bool HasWidget => !(_widget is null);

        public void SetWidgetType<T>() where T : Widget
        {
            _widget = Widget.Find<T>();
        }

        public virtual void ShowWidget(bool ignoreShowAnimation = false)
        {
            if (_widget is null || !IsWidgetControllable)
            {
                return;
            }

            _widget.Show(ignoreShowAnimation);
            _disposableForWidgetControllable =
                _widget.OnDisableObservable.Subscribe(_ =>
                    _toggleListener?.RequestToggledOff(this));
        }

        public virtual void HideWidget(bool ignoreHideAnimation = false)
        {
            if (_widget is null || !IsWidgetControllable)
            {
                return;
            }

            _disposableForWidgetControllable?.Dispose();

            if (!_widget.IsActive())
            {
                return;
            }

            if (_widget is ConfirmPopup confirm)
            {
                confirm.NoWithoutCallback();
            }

            if (_widget is InputBoxPopup inputBox)
            {
                inputBox.No();
            }
            else
            {
                _widget.Close(ignoreHideAnimation);
            }
        }

        #endregion

        #region IToggleable

        public string Name => name;

        public bool Toggleable { get; set; }

        public virtual bool IsToggledOn => toggledOnImage.gameObject.activeSelf;

        public virtual void SetToggleListener(IToggleListener toggleListener)
        {
            _toggleListener = toggleListener;
        }

        public virtual void SetToggledOn()
        {
            if (!Toggleable)
            {
                return;
            }

            toggledOffImage.gameObject.SetActive(false);
            toggledOnImage.gameObject.SetActive(true);
            button.targetGraphic = toggledOnImage;

            ShowWidget();
        }

        public virtual void SetToggledOff()
        {
            if (!Toggleable)
            {
                return;
            }

            toggledOffImage.gameObject.SetActive(true);
            toggledOnImage.gameObject.SetActive(false);
            button.targetGraphic = toggledOffImage;

            HideWidget();
        }

        #endregion

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

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
            toggledOffImage.color = imageColor;
            toggledOnImage.color = imageColor;
            if (!string.IsNullOrEmpty(localizationKey))
            {
                toggledOffText.color = _originalTextColor * imageColor;
                toggledOffText.color = _originalTextColor * imageColor;
            }
        }

        public void SetSortOrderToTop()
        {
            if (!sortingGroup)
            {
                return;
            }

            var systemInfoSortingOrder =
                MainCanvas.instance.GetLayer(WidgetType.System).root.sortingOrder;
            sortingGroup.sortingOrder = systemInfoSortingOrder;
        }

        public void SetSortOrderToNormal()
        {
            if (!sortingGroup)
            {
                return;
            }

            var widget = GetComponentInParent<Widget>();
            if (widget)
            {
                var layerSortingOrder =
                    MainCanvas.instance.GetLayer(widget.WidgetType).root.sortingOrder;
                sortingGroup.sortingOrder = layerSortingOrder + _originalSortingOrderOffset;
            }
            else
            {
                sortingGroup.sortingOrder = _originalSortingOrderOffset;
            }
        }

        protected virtual void SetText(string text)
        {
            if (!(toggledOffText is null))
            {
                toggledOffText.text = text;
            }

            if (!(toggledOnText is null))
            {
                toggledOnText.text = text;
            }
        }

        private void SubscribeOnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClick.OnNext(this);
            _toggleListener?.OnToggle(this);
        }
    }
}
