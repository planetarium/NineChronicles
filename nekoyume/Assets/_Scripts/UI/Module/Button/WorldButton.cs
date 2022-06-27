using System;
using System.Numerics;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.TableData;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class WorldButton : MonoBehaviour
    {
        private enum State
        {
            Unlocked,
            Locked,
            Unlockable,
        }

        private enum AnimationState
        {
            None,
            Idle,
            Hover,
            OpenLock,
        }

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Image grayImage = null;

        [SerializeField]
        private Image colorImage = null;

        [SerializeField, Tooltip("마우스 호버 상태일 때 월드 버튼이 스케일 되는 크기")]
        private float hoverScaleTo = 1.1f;

        [SerializeField, Tooltip("마우스 호버 상태일 때 월드 버튼이 스케일 되는 속도")]
        private float hoverScaleSpeed = 0.7f;

        [SerializeField]
        private GameObject hasNotificationImage = null;

        [SerializeField]
        private string worldName = null;

        [SerializeField]
        private GameObject lockImage = null;

        [SerializeField]
        private GameObject unlockImage = null;

        [SerializeField]
        private GameObject unlockableImage;

        [SerializeField]
        private TMP_Text openCostText;

        [SerializeField]
        private Animator animator;

        private readonly ReactiveProperty<State> _state = new ReactiveProperty<State>(State.Locked);

        private readonly ReactiveProperty<AnimationState> _animationState =
            new ReactiveProperty<AnimationState>(AnimationState.None);

        private Tweener _tweener;
        private BigInteger _openCost;

        public readonly Subject<WorldButton> OnClickSubject = new Subject<WorldButton>();
        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

        public bool IsShown => gameObject.activeSelf;
        private bool IsLocked => _state.Value == State.Locked;
        public bool IsUnlockable => _state.Value == State.Unlockable;
        public string WorldName => worldName;
        public int Id { get; private set; }
        public int StageBegin { get; private set; }
        public int StageEnd { get; private set; }

        private void Awake()
        {
            var go = gameObject;
            go.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x =>
                {
                    _animationState.SetValueAndForceNotify(IsLocked
                        ? AnimationState.None
                        : AnimationState.Hover);
                })
                .AddTo(go);

            go.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x =>
                {
                    _animationState.SetValueAndForceNotify(IsLocked
                        ? AnimationState.None
                        : AnimationState.Idle);
                })
                .AddTo(go);

            button.OnClickAsObservable().Subscribe(OnClick).AddTo(go);
            HasNotification.SubscribeTo(hasNotificationImage).AddTo(go);
            _state.Subscribe(OnState).AddTo(go);
            _animationState.Subscribe(OnAnimationState).AddTo(go);
        }

        private void OnEnable()
        {
            _state.SetValueAndForceNotify(_state.Value);
            _animationState.SetValueAndForceNotify(_animationState.Value);
        }

        private void OnDisable()
        {
            _tweener?.Kill();
            _tweener = null;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Unlock(bool crystalLock = false)
        {
            _state.SetValueAndForceNotify(crystalLock ? State.Unlockable : State.Unlocked);
        }

        public void Lock()
        {
            _state.SetValueAndForceNotify(State.Locked);
        }

        private void OnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClickSubject.OnNext(this);
        }

        private void OnState(State state)
        {
            switch (state)
            {
                case State.Unlocked:
                    button.interactable = true;
                    grayImage.enabled = false;
                    colorImage.enabled = true;
                    lockImage.SetActive(false);
                    unlockImage.SetActive(true);
                    _animationState.SetValueAndForceNotify(AnimationState.OpenLock);
                    break;
                case State.Locked:
                    button.interactable = false;
                    grayImage.enabled = true;
                    colorImage.enabled = false;
                    lockImage.SetActive(true);
                    unlockImage.SetActive(false);
                    unlockableImage.SetActive(false);
                    _animationState.SetValueAndForceNotify(AnimationState.None);
                    break;
                case State.Unlockable:
                    button.interactable = true;
                    unlockableImage.SetActive(true);
                    lockImage.SetActive(false);
                    unlockImage.SetActive(false);
                    animator.Play("WorldOpen");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void OnAnimationState(AnimationState state)
        {
            _tweener?.Kill();
            _tweener = null;

            transform.localScale = UnityEngine.Vector3.one;

            if (_state.Value == State.Locked)
            {
                return;
            }

            switch (state)
            {
                case AnimationState.None:
                    break;
                case AnimationState.Idle:
                    break;
                case AnimationState.Hover:
                    _tweener = transform
                        .DOScale(hoverScaleTo, 1f / hoverScaleSpeed)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;
                case AnimationState.OpenLock:
                    animator.Play("ChainOpen");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public void Set(WorldSheet.Row worldRow)
        {
            Id = worldRow.Id;
            StageBegin = worldRow.StageBegin;
            StageEnd = worldRow.StageEnd;
            if (openCostText != null)
            {
                _openCost = CrystalCalculator
                    .CalculateWorldUnlockCost(new[] {Id}, TableSheets.Instance.WorldUnlockSheet)
                    .MajorUnit;
                openCostText.text = _openCost.ToString();
            }
        }

        public void SetOpenCostTextColor(BigInteger balance)
        {
            if (openCostText != null)
            {
                openCostText.color = Palette.GetColor(balance >= _openCost
                    ? ColorType.ButtonEnabled
                    : ColorType.TextDenial);
            }
        }
    }
}
