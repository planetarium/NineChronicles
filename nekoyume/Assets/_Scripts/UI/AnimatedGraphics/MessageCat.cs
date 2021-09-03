using System;
using Nekoyume.Constraints;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Nekoyume.UI.AnimatedGraphics
{
    [RequireComponent(typeof(RectTransform))]
    public class MessageCat : MonoBehaviour
    {
        private const string ShowString = "Show";
        private static readonly int ShowHash = Animator.StringToHash(ShowString);

        [SerializeField]
        private Animator animator = null;

        [SerializeField]
        private Transform messageTransform = null;

        [SerializeField]
        private TextMeshProUGUI messageText = null;

        [SerializeField]
        private ConstraintsToMousePosition constraintsToMousePosition = null;

        private GameObject _gameObject;
        private RectTransform _rectTransform;
        private Vector3 _originTransformLocalScale;
        private Vector3 _originMessageTransformLocalScale;

        public bool IsShown => _gameObject.activeSelf;

        public GameObject CreatedByThisObject { get; private set; } = null;

        private void Awake()
        {
            _gameObject = gameObject;
            _rectTransform = GetComponent<RectTransform>();
            _originTransformLocalScale = _rectTransform.localScale;
            _originMessageTransformLocalScale = messageTransform.localScale;
        }

        public void Show(Vector3 position, string message, GameObject maker, bool reverseDirection = false)
        {
            CreatedByThisObject = maker;
            constraintsToMousePosition.enabled = false;
            _rectTransform.position = position;
            PostShow(message, reverseDirection);
        }

        public void Show(bool followMouse, string message, GameObject maker, bool reverseDirection = false)
        {
            CreatedByThisObject = maker;
            constraintsToMousePosition.enabled = followMouse;
            PostShow(message, reverseDirection);
        }

        private void PostShow(string message, bool reverseDirection = false)
        {
            _gameObject.SetActive(true);
            messageText.text = message;
            SetDirection(reverseDirection);
            animator.Play("Show");
        }

        public void Jingle()
        {
            animator.Play("Jingle");
        }

        public void Hide(bool lazyHide = true)
        {
            constraintsToMousePosition.enabled = false;

            if (lazyHide)
            {
                animator.Play("Hide");
            }
            else
            {
                _gameObject.SetActive(false);
            }
        }

        private void SetDirection(bool reverseDirection)
        {
            if (reverseDirection)
            {
                _rectTransform.localScale = new Vector3(-_originTransformLocalScale.x,
                    _originTransformLocalScale.y,
                    _originTransformLocalScale.z);
                messageTransform.localScale = new Vector3(-_originMessageTransformLocalScale.x,
                    _originMessageTransformLocalScale.y, _originMessageTransformLocalScale.z);
            }
            else
            {
                _rectTransform.localScale = new Vector3(_originTransformLocalScale.x,
                    _originTransformLocalScale.y,
                    _originTransformLocalScale.z);
                messageTransform.localScale = new Vector3(_originMessageTransformLocalScale.x,
                    _originMessageTransformLocalScale.y, _originMessageTransformLocalScale.z);
            }
        }

        private void OnAnimationEvent(string eventName)
        {
            switch (eventName)
            {
                case "ShowEnd":
                    break;
                case "HideEnd":
                    _gameObject.SetActive(false);
                    break;
            }
        }
    }
}
