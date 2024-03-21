using UnityEngine;
using UnityEngine.UI;
using System;

namespace Nekoyume.UI.Module
{
    [Serializable]
    public class Toggle : UnityEngine.UI.Toggle
    {
        public GameObject offObject;
        public GameObject onObject;
        public UnityEngine.Events.UnityEvent onClickToggle;
        public bool allowSwitchOffWhenIsOn = true;
        public bool obsolete = false;
        public UnityEngine.Events.UnityEvent onClickObsoletedToggle;

        [SerializeField]
        [Tooltip("Graphics that will have color changes applied to them when the toggle state changes.")]
        private Graphic[] colorTransitionGraphics;

        protected Toggle()
        {
            onValueChanged.AddListener(UpdateObject);
        }

        protected virtual void UpdateObject(bool value)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif

            if (onObject)
            {
                onObject.SetActive(isOn);
            }

            if (offObject)
            {
                offObject.SetActive(!isOn);
            }
        }

        protected override void OnDestroy()
        {
            onValueChanged.RemoveAllListeners();
            base.OnDestroy();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (isActiveAndEnabled && !Application.isPlaying)
            {
                StartColorTweens(Color.white, true);
            }
        }
#endif // if UNITY_EDITOR

        protected override void InstantClearState()
        {
            base.InstantClearState();

            if (transition == Transition.ColorTint)
            {
                StartColorTweens(Color.white, true);
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            Color tintColor;
            switch (state)
            {
                case SelectionState.Normal:
                    tintColor = colors.normalColor;
                    break;
                case SelectionState.Highlighted:
                    tintColor = colors.highlightedColor;
                    break;
                case SelectionState.Pressed:
                    tintColor = colors.pressedColor;
                    break;
                case SelectionState.Selected:
                    tintColor = colors.selectedColor;
                    break;
                case SelectionState.Disabled:
                    tintColor = colors.disabledColor;
                    break;
                default:
                    tintColor = Color.black;
                    break;
            }

            if (transition == Transition.ColorTint)
            {
                StartColorTweens(tintColor * colors.colorMultiplier, instant);
            }
        }

        protected virtual void StartColorTweens(Color targetColor, bool instant)
        {
            if (colorTransitionGraphics == null)
                return;

            foreach (var targetColorGraphic in colorTransitionGraphics)
            {
                targetColorGraphic.CrossFadeColor(targetColor, instant ? 0f : colors.fadeDuration, true, true);
            }
        }

        public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!allowSwitchOffWhenIsOn && isOn)
            {
                return;
            }

            if (obsolete)
            {
                onClickObsoletedToggle?.Invoke();
                return;
            }

            base.OnPointerClick(eventData);
            onClickToggle?.Invoke();
        }
    }
}
