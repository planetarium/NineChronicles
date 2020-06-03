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
        protected enum AnimationStateType
        {
            Showing,
            Shown,
            Closing,
            Closed
        }

        private struct PoolElementModel
        {
            public GameObject gameObject;
            public Widget widget;
        }

        private static readonly Subject<Widget> OnEnableStaticSubject = new Subject<Widget>();
        private static readonly Subject<Widget> OnDisableStaticSubject = new Subject<Widget>();

        private static readonly Dictionary<Type, PoolElementModel> Pool =
            new Dictionary<Type, PoolElementModel>();

        private static readonly Stack<GameObject> WidgetStack = new Stack<GameObject>();

        public static IObservable<Widget> OnEnableStaticObservable => OnEnableStaticSubject;

        public static IObservable<Widget> OnDisableStaticObservable => OnDisableStaticSubject;

        /// <summary>
        /// AnimationState 캡슐화가 깨지는 setter를 사용하지 않도록 한다.
        /// BottomMenu에서만 예외적으로 사용하고 있는데, 이를 Widget 안으로 옮긴 후에 setter를 private으로 변경한다.
        /// </summary>
        protected AnimationStateType AnimationState { get; set; } = AnimationStateType.Closed;

        private readonly Subject<Widget> _onEnableSubject = new Subject<Widget>();
        private readonly Subject<Widget> _onDisableSubject = new Subject<Widget>();

        private Coroutine _coClose;

        protected System.Action CloseWidget;
        protected System.Action SubmitWidget;

        protected virtual WidgetType WidgetType => WidgetType.Widget;

        protected RectTransform RectTransform { get; private set; }

        protected Animator Animator { get; private set; }

        public bool IsCloseAnimationCompleted { get; private set; }

        public IObservable<Widget> OnEnableObservable => _onEnableSubject;

        public IObservable<Widget> OnDisableObservable => _onDisableSubject;

        protected virtual bool CanHandleInputEvent => AnimationState == AnimationStateType.Shown;

        protected bool CanClose => CanHandleInputEvent;

        #region Mono

        protected virtual void Awake()
        {
            Animator = GetComponent<Animator>();
            RectTransform = GetComponent<RectTransform>();

            CloseWidget = () => Close();
            SubmitWidget = null;
        }

        protected virtual void Update()
        {
            CheckInput();
        }

        protected virtual void OnEnable()
        {
            OnEnableStaticSubject.OnNext(this);
            _onEnableSubject.OnNext(this);
        }

        protected virtual void OnDisable()
        {
            OnDisableStaticSubject.OnNext(this);
            _onDisableSubject.OnNext(this);
        }

        protected virtual void OnDestroy()
        {
            _onEnableSubject.Dispose();
            _onDisableSubject.Dispose();
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
                    Pool.Add(type, new PoolElementModel
                    {
                        gameObject = go,
                        widget = widget
                    });
                    break;
                case WidgetType.Hud:
                case WidgetType.Animation:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            go.transform.SetParent(MainCanvas.instance.GetLayerRootTransform(widget.WidgetType));
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

        public static T FindOrCreate<T>() where T : HudWidget
        {
            var type = typeof(T);
            var names = type.ToString().Split('.');
            var widgetName = $"UI_{names[names.Length - 1]}";
            var resName = $"UI/Prefabs/{widgetName}";
            var pool = Game.Game.instance.Stage.objectPool;
            var go = pool.Get(widgetName, false);
            if (go is null)
            {
                Debug.Log("create new");
                var res = Resources.Load<GameObject>(resName);
                var go2 = Instantiate(res, MainCanvas.instance.transform);
                go2.name = widgetName;
                pool.Add(go2, 1);
                return go2.GetComponent<T>();
            }

            go.transform.SetParent(MainCanvas.instance.GetLayerRootTransform(WidgetType.Hud));
            return go.GetComponent<T>();
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

        public virtual void Show(bool ignoreShowAnimation = false)
        {
            if (CloseWidget != null ||
                SubmitWidget != null ||
                WidgetType == WidgetType.Screen)
            {
                WidgetStack.Push(gameObject);
            }

            if (WidgetType == WidgetType.Screen)
            {
                MainCanvas.instance.SetLayerSortingOrderToTarget(
                    WidgetType.Screen,
                    WidgetType.Popup);
            }
            else if (WidgetType == WidgetType.Popup)
            {
                MainCanvas.instance.SetLayerSortingOrderToTarget(
                    WidgetType.Popup,
                    WidgetType.Screen);
            }

            AnimationState = AnimationStateType.Showing;
            gameObject.SetActive(true);

            if (!Animator ||
                ignoreShowAnimation)
            {
                AnimationState = AnimationStateType.Shown;
                return;
            }

            Animator.enabled = true;
            Animator.Play("Show", 0, 0);
        }

        public virtual void Close(bool ignoreCloseAnimation = false)
        {
            if (WidgetStack.Count != 0 &&
                WidgetStack.Peek() == gameObject)
            {
                WidgetStack.Pop();
            }

            if (!gameObject.activeSelf)
            {
                return;
            }

            if (!Animator ||
                ignoreCloseAnimation)
            {
                OnCompleteOfCloseAnimation();
                gameObject.SetActive(false);
                AnimationState = AnimationStateType.Closed;
                return;
            }

            AnimationState = AnimationStateType.Closing;
            // TODO : wait close animation
            if (!(_coClose is null))
            {
                StopCoroutine(_coClose);
                _coClose = null;
            }

            StartCoroutine(CoClose());
        }

        protected void Push()
        {
            if (CloseWidget != null ||
                SubmitWidget != null ||
                WidgetType == WidgetType.Screen)
            {
                WidgetStack.Push(gameObject);
            }
        }

        protected void Pop()
        {
            if (WidgetStack.Count != 0 &&
                WidgetStack.Peek() == gameObject)
            {
                WidgetStack.Pop();
            }
        }

        public virtual IEnumerator CoClose()
        {
            if (Animator)
            {
                IsCloseAnimationCompleted = false;
                Animator.enabled = true;
                Animator.Play("Close", 0, 0);
                _coClose = StartCoroutine(CoCompleteCloseAnimation());
                yield return new WaitUntil(() => IsCloseAnimationCompleted);
                StopCoroutine(_coClose);
                _coClose = null;
            }

            gameObject.SetActive(false);
            AnimationState = AnimationStateType.Closed;
        }

        private IEnumerator CoCompleteCloseAnimation()
        {
            yield return new WaitForSeconds(1f);
            if (!IsCloseAnimationCompleted)
            {
                IsCloseAnimationCompleted = true;
                AnimationState = AnimationStateType.Closed;
            }
        }

        #region Call From Animation

        private void OnCompleteOfShowAnimation()
        {
            OnCompleteOfShowAnimationInternal();
            AnimationState = AnimationStateType.Shown;
        }

        protected virtual void OnCompleteOfShowAnimationInternal()
        {
        }

        private void OnCompleteOfCloseAnimation()
        {
            OnCompleteOfCloseAnimationInternal();

            IsCloseAnimationCompleted = true;
        }

        protected virtual void OnCompleteOfCloseAnimationInternal()
        {
        }

        #endregion

        private void CheckInput()
        {
            if (!CanHandleInputEvent)
            {
                return;
            }

            if (WidgetStack.Count == 0 ||
                WidgetStack.Peek() != gameObject)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideAllMessageCat();
                CloseWidget?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                HideAllMessageCat();
                SubmitWidget?.Invoke();
            }
        }

        private static void HideAllMessageCat()
        {
            try
            {
                Find<MessageCatManager>().HideAll(false);
            }
            catch (WidgetNotFoundException)
            {
                // Do Nothing.
            }
        }
    }
}
