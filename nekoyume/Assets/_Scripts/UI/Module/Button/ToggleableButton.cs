using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using System;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
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
        private Canvas sortingGroup;

        public string localizationKey = null;

        private IToggleListener _toggleListener;

        private Animator _animatorCache;
        private Color _originalTextColor;

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

            Toggleable = true;
            IsWidgetControllable = true;

            button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);
            onPointerEnter = gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable();
            onPointerExit = gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable();

            if (!string.IsNullOrEmpty(localizationKey))
            {
                var text = LocalizationManager.Localize(localizationKey);
                toggledOffText.text = text;
                toggledOnText.text = text;
            }

            sortingGroup.sortingLayerName = "UI";
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

            if (_widget is Confirm confirm)
            {
                confirm.NoWithoutCallback();
            }

            if (_widget is InputBox inputBox)
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
            sortingGroup.sortingOrder = 100;
        }

        public void SetSortOrderToNormal()
        {
            sortingGroup.sortingOrder = 0;
        }

        private void SubscribeOnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClick.OnNext(this);
            _toggleListener?.OnToggle(this);
        }
    }
}
