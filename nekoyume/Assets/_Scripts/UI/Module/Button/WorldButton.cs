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
        public enum WorldState
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

        private readonly ReactiveProperty<WorldState> _state = new(WorldState.Locked);

        private readonly ReactiveProperty<AnimationState> _animationState = new(AnimationState.None);

        private Tweener _tweener;
        private BigInteger _openCost;
        private bool _interactable;

        public readonly Subject<WorldButton> OnClickSubject = new();
        public readonly ReactiveProperty<bool> HasNotification = new(false);

        public bool IsShown => gameObject.activeSelf;
        private bool IsLocked => _state.Value == WorldState.Locked;
        public bool IsUnlockable => _state.Value == WorldState.Unlockable;
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
            _state.Subscribe(OnEnterWorldButtonState).AddTo(go);
            _animationState.Subscribe(OnAnimationState).AddTo(go);
        }

        private void OnEnable()
        {
            _state.SetValueAndForceNotify(_state.Value);
            _animationState.SetValueAndForceNotify(AnimationState.None);
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
            _interactable = true;
            _state.SetValueAndForceNotify(crystalLock ? WorldState.Unlockable : WorldState.Unlocked);
        }

        public void Lock(bool interactable = false)
        {
            _interactable = interactable;
            _state.SetValueAndForceNotify(WorldState.Locked);
        }

        public void OnCompleteAnimation(WorldState worldState)
        {
            switch (worldState)
            {
                case WorldState.Locked:
                case WorldState.Unlockable:
                    break;
                case WorldState.Unlocked:
                    unlockableImage.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(worldState), worldState, null);
            }
        }

        private void OnClick(Unit unit)
        {
            AudioController.PlayClick();
            OnClickSubject.OnNext(this);
        }

        private void OnEnterWorldButtonState(WorldState worldState)
        {
            button.interactable = _interactable;
            switch (worldState)
            {
                case WorldState.Unlocked:
                    grayImage.enabled = false;
                    colorImage.enabled = true;
                    lockImage.SetActive(false);
                    unlockImage.SetActive(true);
                    // unlockableImage not set.
                    animator.Play(worldState.ToString());
                    break;
                case WorldState.Locked:
                    grayImage.enabled = true;
                    colorImage.enabled = false;
                    lockImage.SetActive(true);
                    unlockImage.SetActive(false);
                    unlockableImage.SetActive(false);
                    break;
                case WorldState.Unlockable:
                    lockImage.SetActive(false);
                    unlockImage.SetActive(false);
                    unlockableImage.SetActive(true);
                    animator.Play(worldState.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(worldState), worldState, null);
            }
        }

        private void OnAnimationState(AnimationState state)
        {
            _tweener?.Kill();
            _tweener = null;

            transform.localScale = UnityEngine.Vector3.one;

            if (_state.Value == WorldState.Locked)
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
