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

        public static readonly Subject<Widget> OnEnableStaticSubject = new Subject<Widget>();
        public static readonly Subject<Widget> OnDisableStaticSubject = new Subject<Widget>();

        public readonly Subject<Widget> OnEnableSubject = new Subject<Widget>();
        public readonly Subject<Widget> OnDisableSubject = new Subject<Widget>();

        private Animator _animator;

        private static readonly Dictionary<Type, PoolElementModel> Pool = new Dictionary<Type, PoolElementModel>();
        private static readonly Stack<GameObject> WidgetStack = new Stack<GameObject>();
        private bool _isCloseAnimationCompleted;
        
        protected System.Action CloseWidget;
        protected System.Action SubmitWidget;

        public RectTransform RectTransform { get; private set; }
        public virtual WidgetType WidgetType => WidgetType.Widget;

        #region Mono

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            RectTransform = GetComponent<RectTransform>();

            CloseWidget = () => Close();
            SubmitWidget = null;
        }

        protected virtual void Update()
        {
            if (WidgetStack.Count == 0 || WidgetStack.Peek() != gameObject)
                return;
            
            if(Input.GetKeyUp(KeyCode.Escape))
                CloseWidget?.Invoke();
            if (Input.GetKeyUp(KeyCode.Return))
                SubmitWidget?.Invoke();
        }

        protected virtual void OnEnable()
        {
            OnEnableStaticSubject.OnNext(this);
            OnEnableSubject.OnNext(this);
        }

        protected virtual void OnDisable()
        {
            OnDisableStaticSubject.OnNext(this);
            OnDisableSubject.OnNext(this);
        }

        protected virtual void OnDestroy()
        {
            OnEnableSubject.Dispose();
            OnDisableSubject.Dispose();
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
            if (res is null)
                throw new FailedToLoadResourceException<GameObject>(resName);
            
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
                case WidgetType.Development:
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

        public static T Find<T>() where T : Widget
        {
            var type = typeof(T);
            if (!Pool.TryGetValue(type, out var model))
            {
                throw new WidgetNotFoundException(type.Name);
            }

            return (T) model.widget;
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
            if(CloseWidget != null || SubmitWidget != null || WidgetType == WidgetType.Screen)
                WidgetStack.Push(gameObject);
            
            if (WidgetType == WidgetType.Screen)
            {
                MainCanvas.instance.SetSiblingOrderNext(WidgetType, WidgetType.Popup);
            }
            else if (WidgetType == WidgetType.Popup)
            {
                MainCanvas.instance.SetSiblingOrderNext(WidgetType, WidgetType.Screen);
            }

            gameObject.SetActive(true);
            if (_animator)
            {
                _animator.enabled = true;
                _animator.Play("Show");
            }
        }
        
        public virtual void Close(bool ignoreCloseAnimation = false)
        {
            if(WidgetStack.Count != 0 && WidgetStack.Peek() == gameObject) 
                WidgetStack.Pop();
            
            StopAllCoroutines();
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (ignoreCloseAnimation)
            {
                OnCompleteOfCloseAnimation();
                gameObject.SetActive(false);
                return;
            }

            // TODO : wait close animation
            StartCoroutine(CoClose());
        }

        protected void Push()
        {
            if(CloseWidget != null || SubmitWidget != null || WidgetType == WidgetType.Screen)
                WidgetStack.Push(gameObject);
        }

        protected void Pop()
        {
            if(WidgetStack.Count != 0 && WidgetStack.Peek() == gameObject) 
                WidgetStack.Pop();
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

        #region Call From Animation

        protected virtual void OnCompleteOfShowAnimation()
        {
            if (_animator)
            {
                _animator.enabled = false;
            }
        }

        protected virtual void OnCompleteOfCloseAnimation()
        {
            if (_animator)
            {
                _animator.enabled = false;
            }

            _isCloseAnimationCompleted = true;
        }
        
        #endregion
    }
}
