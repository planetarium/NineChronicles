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

        private static readonly Subject<Widget> OnEnableStaticSubject = new Subject<Widget>();
        private static readonly Subject<Widget> OnDisableStaticSubject = new Subject<Widget>();

        private static readonly Dictionary<Type, PoolElementModel> Pool =
            new Dictionary<Type, PoolElementModel>();

        protected static readonly Stack<GameObject> WidgetStack = new Stack<GameObject>();

        public static IObservable<Widget> OnEnableStaticObservable => OnEnableStaticSubject;

        public static IObservable<Widget> OnDisableStaticObservable => OnDisableStaticSubject;

        protected readonly ReactiveProperty<AnimationStateType> AnimationState =
            new ReactiveProperty<AnimationStateType>(AnimationStateType.Closed);

        private readonly Subject<Widget> _onEnableSubject = new Subject<Widget>();
        private readonly Subject<Widget> _onDisableSubject = new Subject<Widget>();
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

        public List<TutorialTarget> tutorialTargets = new List<TutorialTarget>();
        public List<TutorialActionType> tutorialActions = new List<TutorialActionType>();

        public IObservable<Widget> OnEnableObservable => _onEnableSubject;

        public IObservable<Widget> OnDisableObservable => _onDisableSubject;

        public virtual bool CanHandleInputEvent => AnimationState.Value == AnimationStateType.Shown;

        protected bool CanClose => CanHandleInputEvent;

        #region Mono

        protected virtual void Awake()
        {
            Animator = GetComponent<Animator>();
            RectTransform = GetComponent<RectTransform>();

            CloseWidget = () => Close();
            SubmitWidget = null;

            AnimationState.Subscribe(stateType =>
            {
                var fields = GetType().GetFields(System.Reflection.BindingFlags.NonPublic |
                                                 System.Reflection.BindingFlags.Instance);
                foreach (var selectable in fields
                    .Select(field => field.GetValue(this))
                    .OfType<UnityEngine.UI.Selectable>())
                {
                    selectable.interactable = stateType == AnimationStateType.Shown;
                }
            }).AddTo(gameObject);

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
                case WidgetType.Development:
                    Pool.Add(type, new PoolElementModel
                    {
                        gameObject = go,
                        widget = widget
                    });
                    break;
                case WidgetType.Hud:
                case WidgetType.Animation:
                case WidgetType.TutorialMask:
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
                throw new WidgetNotFoundException(type.Name);
            }

            return (T) model.widget;
        }

        public static bool TryFind<T>(out T widget) where T : Widget
        {
            widget = null;
            var type = typeof(T);
            if (!Pool.TryGetValue(type, out var model))
            {
                return false;
            }

            widget = (T) model.widget;
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
                go.transform.SetParent(MainCanvas.instance.GetLayerRootTransform(widget.WidgetType));
                return widget;
            }
            else
            {
                Debug.Log("create new");
                var prefab = Resources.Load<GameObject>(resName);
                go = Instantiate(prefab, MainCanvas.instance.RectTransform);
                go.name = widgetName;
                pool.Add(go, 1);
                var widget = go.GetComponent<T>();
                go.transform.SetParent(MainCanvas.instance.GetLayerRootTransform(widget.WidgetType));

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
            if (!(_coClose is null))
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

            if (!Animator || ignoreShowAnimation)
            {
                AnimationState.Value = AnimationStateType.Shown;
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
            if (!(_coClose is null))
            {
                StopCoroutine(_coClose);
                _coClose = null;
            }

            if (!(_coCompleteCloseAnimation is null))
            {
                StopCoroutine(_coCompleteCloseAnimation);
                _coCompleteCloseAnimation = null;
            }

            if (isActiveAndEnabled)
            {
                _coClose = StartCoroutine(CoClose());
            }
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

                if (!(_coCompleteCloseAnimation is null))
                {
                    StopCoroutine(_coCompleteCloseAnimation);
                }

                _coCompleteCloseAnimation = StartCoroutine(CoCompleteCloseAnimation());
                yield return new WaitUntil(() => IsCloseAnimationCompleted);
                if (!(_coCompleteCloseAnimation is null))
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
            var deletableWidgets = FindWidgets().Where(widget =>
                !(widget is SystemWidget) &&
                !(widget is MessageCatTooltip) &&
                !(widget is HeaderMenuStatic) &&
                !(widget is MaterialTooltip) &&
                !(widget is ShopBuy) &&
                !(widget is ShopSell) &&
                widget.IsActive());
            foreach (var widget in deletableWidgets)
            {
                widget.Close(true);
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
                AnimationState.Value = AnimationStateType.Closed;
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
                WidgetHandler.Instance.HideAllMessageCat();
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
    }
}
