using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Widget : MonoBehaviour
    {
        private struct PoolElementModel
        {
            public GameObject gameObject;
            public Widget widget;
        }

        protected static readonly Subject<Widget> OnEnableSubject = new Subject<Widget>();
        protected static readonly Subject<Widget> OnDisableSubject = new Subject<Widget>();

        private static readonly Dictionary<Type, PoolElementModel> Pool = new Dictionary<Type, PoolElementModel>();
        private static readonly int Radius = Shader.PropertyToID("_Radius");

        private Material _glass;
        private Animator _animator;
        private bool _isCloseAnimationCompleted;

        public RectTransform RectTransform { get; private set; }
        public virtual WidgetType WidgetType => WidgetType.Widget;

        #region Mono

        protected virtual void Awake()
        {
            FindGlassMaterial(gameObject);

            _animator = GetComponent<Animator>();
            RectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            OnEnableSubject.OnNext(this);
        }

        private void OnDisable()
        {
            OnDisableSubject.OnNext(this);
        }

        #endregion

        public virtual void Initialize()
        {
        }

        public static T Create<T>(bool activate = false) where T : Widget
        {
            var type = typeof(T);
            var names = type.ToString().Split('.');
            var resName = $"UI/Prefabs/UI_{names[names.Length - 1]}";
            var res = Resources.Load<GameObject>(resName);
            if (!ReferenceEquals(res, null))
            {
                if (Pool.ContainsKey(type))
                {
                    Debug.LogWarning($"Duplicated create widget: {type}");
                    Pool[type].gameObject.SetActive(activate);

                    return (T) Pool[type].widget;
                }

                var go = Instantiate(res, MainCanvas.instance.transform);
                var widget = go.GetComponent<T>();
                switch (widget.WidgetType)
                {
                    case WidgetType.Popup:
                    case WidgetType.Screen:
                    case WidgetType.Tooltip:
                    case WidgetType.Widget:
                    case WidgetType.SystemInfo:
                        go.transform.SetParent(MainCanvas.instance.widget.transform);
                        go.SetActive(activate);
                        Pool.Add(type, new PoolElementModel
                        {
                            gameObject = go,
                            widget = widget
                        });
                        break;
                    case WidgetType.Hud:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                go.transform.SetParent(MainCanvas.instance.GetTransform(widget.WidgetType));
                go.SetActive(activate);
                return widget;
            }

            Debug.LogWarning(($"widget not exist: {type}"));

            return null;
        }

        public static T Find<T>() where T : Widget
        {
            var type = typeof(T);
            if (!Pool.TryGetValue(type, out var model))
            {
                throw new WidgetNotFoundException(type.Name);
            }

            return (T) model.widget;
        }


        private void FindGlassMaterial(GameObject go)
        {
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (!image || !image.material || image.material.shader.name != "UI/Unlit/FrostedGlass")
            {
                return;
            }

            _glass = image.material;
        }
        
        public virtual bool IsActive()
        {
            return gameObject.activeSelf;
        }
        
        public void Toggle()
        {
            if (IsActive())
            {
                Close();
            }
            else
            {
                Show();
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (_animator)
            {
                _animator.enabled = true;
                _animator.Play("Show");
            }

            StartCoroutine(Blur());
        }
        
        public virtual void Close()
        {
            StopAllCoroutines();
            if (!gameObject.activeSelf)
            {
                return;
            }

            // TODO : wait close animation
            StartCoroutine(CoClose());
        }
        
        public virtual IEnumerator CoClose()
        {
            if (_animator)
            {
                _isCloseAnimationCompleted = false;
                _animator.enabled = true;
                _animator.Play("Close");
                yield return new WaitUntil(() => _isCloseAnimationCompleted);
            }

            gameObject.SetActive(false);
        }

        private IEnumerator Blur()
        {
            if (!_glass)
            {
                yield break;
            }

            var from = 0f;
            var to = _glass.GetFloat(Radius);

            _glass.SetFloat(Radius, from);
            var time = 0f;
            while (true)
            {
                var current = Mathf.Lerp(from, to, time);
                _glass.SetFloat(Radius, current);

                time += Time.deltaTime * 3f;
                if (time > 1f ||
                    !gameObject.activeInHierarchy)
                {
                    break;
                }

                yield return null;
            }

            _glass.SetFloat(Radius, to);
        }

        #region Call From Animation
        
        public virtual void OnCompleteOfShowAnimation()
        {
            _animator.enabled = false;
        }

        public virtual void OnCompleteOfCloseAnimation()
        {
            _animator.enabled = false;
            _isCloseAnimationCompleted = true;
        }
        
        #endregion
    }
}
