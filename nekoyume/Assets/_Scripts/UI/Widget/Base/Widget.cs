using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

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

        private static readonly Subject<Widget> OnEnableStaticSubject = new();
        private static readonly Subject<Widget> OnDisableStaticSubject = new();

        private static readonly Dictionary<Type, PoolElementModel> Pool = new();

        protected static readonly Stack<GameObject> WidgetStack = new();

        public static IObservable<Widget> OnEnableStaticObservable => OnEnableStaticSubject;

        public static IObservable<Widget> OnDisableStaticObservable => OnDisableStaticSubject;

        protected readonly ReactiveProperty<AnimationStateType> AnimationState =
            new(AnimationStateType.Closed);

        private readonly Subject<Widget> _onEnableSubject = new();
        private readonly Subject<Widget> _onDisableSubject = new();
        private System.Action _onClose;

        private Coroutine _coClose;
        private Coroutine _coCompleteCloseAnimation;

        protected System.Action CloseWidget;
        protected System.Action SubmitWidget;

        public virtual WidgetType WidgetType => WidgetType.Widget;
        public virtual CloseKeyType CloseKeyType => CloseKeyType.Backspace;

        protected RectTransform RectTransform { get; private set; }

        protected Animator Animator { get; private set; }

        public bool IsCloseAnimationCompleted { get; private set; }

        public List<TutorialTarget> tutorialTargets = new();
        public List<TutorialActionType> tutorialActions = new();

        public IObservable<Widget> OnEnableObservable => _onEnableSubject;

        public IObservable<Widget> OnDisableObservable => _onDisableSubject;

        public virtual bool CanHandleInputEvent => AnimationState.Value == AnimationStateType.Shown;

        protected bool CanClose => CanHandleInputEvent;

        #region Mono

        private bool _isClosed;

        protected virtual void Awake()
        {
            Animator = GetComponent<Animator>();
            RectTransform = GetComponent<RectTransform>();

            CloseWidget = () => Close();
            SubmitWidget = null;

            var blur = transform.GetComponentInChildren<Blur>();
            if (blur)
            {
                blur.OnClick = () => Close();
            }
        }

        protected virtual void Update()
        {
            CheckInput();
        }

        protected virtual void OnEnable()
        {
            _isClosed = false;
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
                NcDebug.LogWarning($"Duplicated create widget: {type}");
                Pool[type].gameObject.SetActive(activate);

                return (T)Pool[type].widget;
            }

            var widgetType = res.GetComponent<T>().WidgetType;
            var go = Instantiate(res, MainCanvas.instance.GetLayerRootTransform(widgetType));
            var widget = go.GetComponent<T>();
            switch (widget.WidgetType)
            {
                case WidgetType.Popup:
                case WidgetType.Static:
                case WidgetType.Widget:
                case WidgetType.Tooltip:
                case WidgetType.Screen:
                case WidgetType.System:
                case WidgetType.TutorialMask:
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

            go.SetActive(activate);
            return widget;
        }

        public static T Find<T>() where T : Widget
        {
            var type = typeof(T);
            if (!Pool.TryGetValue(type, out var model))
            {
#if APPLY_MEMORY_IOS_OPTIMIZATION || UNITY_ANDROID || UNITY_IOS
                // Memory optimization
                return MainCanvas.instance.AddWidget<T>();
#else
                throw new WidgetNotFoundException(type.Name);
#endif
            }

            return (T)model.widget;
        }

        public static bool TryFind<T>(out T widget) where T : Widget
        {
            widget = null;
            var type = typeof(T);
            if (!Pool.TryGetValue(type, out var model))
            {
                return false;
            }

            widget = (T)model.widget;
            return true;
        }

        public static IEnumerable<Widget> FindWidgets()
        {
            return Pool.Values.Select(value => value.widget);
        }

        public static T FindOrCreate<T>() where T : Widget
        {
            var type = typeof(T);
            var names = type.ToString().Split('.');
            var widgetName = $"UI_{names[names.Length - 1]}";
            var resName = $"UI/Prefabs/{widgetName}";
            var pool = Game.Game.instance.Stage.objectPool;
            var go = pool.Get(widgetName, false);
            if (go)
            {
                var widget = go.GetComponent<T>();
                go.transform.SetParent(
                    MainCanvas.instance.GetLayerRootTransform(widget.WidgetType));
                return widget;
            }
            else
            {
                NcDebug.Log("create new");
                var prefab = Resources.Load<GameObject>(resName);
                go = Instantiate(prefab, MainCanvas.instance.RectTransform);
                go.name = widgetName;
                pool.Add(go, 1);
                var widget = go.GetComponent<T>();
                go.transform.SetParent(
                    MainCanvas.instance.GetLayerRootTransform(widget.WidgetType));

                return widget;
            }
        }

        public static bool IsOpenAnyPopup()
        {
            foreach (var model in Pool)
            {
                if (model.Value.widget.CloseKeyType == CloseKeyType.Escape &&
                    model.Value.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
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

        public void Show(System.Action onClose, bool ignoreShowAnimation = false)
        {
            _onClose = onClose;
            Show(ignoreShowAnimation);
        }

        public virtual void Show(bool ignoreShowAnimation = false)
        {
            NcDebug.Log($"[Widget][{GetType().Name}] Show({ignoreShowAnimation}) invoked.", channel: "Widget");
            if (_coClose is not null)
            {
                StopCoroutine(_coClose);
                _coClose = null;
            }

            if (CloseWidget != null ||
                SubmitWidget != null ||
                WidgetType == WidgetType.Screen)
            {
                WidgetStack.Push(gameObject);
            }

            AnimationState.Value = AnimationStateType.Showing;
            gameObject.SetActive(true);

#if UNITY_ANDROID || UNITY_IOS
            transform.SetAsLastSibling();
#endif

            if (!Animator || ignoreShowAnimation)
            {
                AnimationState.Value = AnimationStateType.Shown;
                return;
            }

            Animator.enabled = true;
            Animator.Play("Show", 0, 0);
        }

        public void ForceClose(bool ignoreCloseAnimation = false)
        {
            _isClosed = false;
            Close(ignoreCloseAnimation);
        }

        public virtual void Close(bool ignoreCloseAnimation = false)
        {
            NcDebug.Log($"[Widget][{GetType().Name}] Close({ignoreCloseAnimation}) invoked.", channel: "Widget");
            if (WidgetStack.Count > 0 &&
                WidgetStack.Peek() == gameObject)
            {
                WidgetStack.Pop();
            }

            if (!gameObject.activeSelf)
            {
                return;
            }

            if (_isClosed && !ignoreCloseAnimation)
            {
                return;
            }

            _onClose?.Invoke();

            if (!Animator ||
                ignoreCloseAnimation)
            {
                OnCompleteOfCloseAnimation();
                gameObject.SetActive(false);
                AnimationState.Value = AnimationStateType.Closed;

                return;
            }

            AnimationState.Value = AnimationStateType.Closing;
            // TODO : wait close animation
            if (_coClose is not null)
            {
                StopCoroutine(_coClose);
                _coClose = null;
            }

            if (_coCompleteCloseAnimation is not null)
            {
                StopCoroutine(_coCompleteCloseAnimation);
                _coCompleteCloseAnimation = null;
            }

            if (isActiveAndEnabled)
            {
                _coClose = StartCoroutine(CoClose());
            }

            _isClosed = true;
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

                if (_coCompleteCloseAnimation is not null)
                {
                    StopCoroutine(_coCompleteCloseAnimation);
                }

                _coCompleteCloseAnimation = StartCoroutine(CoCompleteCloseAnimation());
                yield return new WaitUntil(() => IsCloseAnimationCompleted);
                if (_coCompleteCloseAnimation is not null)
                {
                    StopCoroutine(_coCompleteCloseAnimation);
                    _coCompleteCloseAnimation = null;
                }
            }

            gameObject.SetActive(false);
            AnimationState.Value = AnimationStateType.Closed;
        }

        public void CloseWithOtherWidgets()
        {
            try
            {
                var deletableWidgets = FindWidgets().Where(widget =>
                    widget is not SystemWidget &&
                    widget is not MessageCatTooltip &&
                    widget is not HeaderMenuStatic &&
                    widget is not MaterialTooltip &&
                    widget is not ShopBuy &&
                    widget is not ShopSell &&
                    widget.IsActive()).ToList();
                for (var i = deletableWidgets.Count - 1; i >= 0; i--)
                {
                    var widget = deletableWidgets[i];
                    if (widget)
                    {
                        widget.Close(true);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Find<Menu>().Close(true);
            Find<ShopBuy>().Close(true, true);
            Find<ShopSell>().Close(true, true);
            Find<EventBanner>().Close(true);
            Find<Status>().Close(true);
            Close(true);
        }

        private IEnumerator CoCompleteCloseAnimation()
        {
            yield return new WaitForSeconds(2f);
            if (!IsCloseAnimationCompleted)
            {
                IsCloseAnimationCompleted = true;
            }
        }

        #region Call From Animation

        private void OnCompleteOfShowAnimation()
        {
            OnCompleteOfShowAnimationInternal();
            AnimationState.Value = AnimationStateType.Shown;
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
#if UNITY_ANDROID
            if (Input.anyKeyDown)
            {
                WidgetHandler.Instance.HideAllMessageCat();
            }
#endif

            if (!CanHandleInputEvent)
            {
                return;
            }

            if (WidgetStack.Count == 0 ||
                WidgetStack.Peek() != gameObject)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                InvokeCloseWidget(KeyCode.Backspace);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                InvokeCloseWidget(KeyCode.Escape);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                SubmitWidget?.Invoke();
            }
        }

        private void InvokeCloseWidget(KeyCode keyCode)
        {
            if (!keyCode.ToString().Equals(CloseKeyType.ToString()))
            {
                return;
            }

            if (!WidgetHandler.Instance.IsActiveTutorialMaskWidget)
            {
                WidgetHandler.Instance.HideAllMessageCat();
                CloseWidget?.Invoke();
            }
        }

        public static bool Remove<T>(T widget) where T : Widget
        {
            Destroy(widget.gameObject);
            return Pool.Remove(typeof(T));
        }
    }
}
