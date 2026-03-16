using System;
using System.Linq;
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

        [SerializeField][Tooltip("마우스 호버 상태일 때 월드 버튼이 스케일 되는 크기")]
        private float hoverScaleTo = 1.1f;

        [SerializeField][Tooltip("마우스 호버 상태일 때 월드 버튼이 스케일 되는 속도")]
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

        public bool IsLockNameShow = true;

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

        private void EnsureOpenCostTextWired()
        {
            if (openCostText != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts == null || texts.Length == 0)
            {
                return;
            }

            static bool LooksNumeric(string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    return false;
                }

                var hasDigit = false;
                foreach (var ch in s)
                {
                    if (char.IsDigit(ch))
                    {
                        hasDigit = true;
                        continue;
                    }

                    if (ch == ',')
                    {
                        continue;
                    }

                    return false;
                }

                return hasDigit;
            }

            TMP_Text best = null;
            var bestScore = float.PositiveInfinity;
            foreach (var t in texts)
            {
                if (t == null)
                {
                    continue;
                }

                var name = t.gameObject != null ? t.gameObject.name : string.Empty;
                var isCostNamed = name.IndexOf("cost", StringComparison.OrdinalIgnoreCase) >= 0;
                var isNumeric = LooksNumeric(t.text);
                if (!isCostNamed && !isNumeric)
                {
                    continue;
                }

                var rt = t.GetComponent<RectTransform>();
                var area = rt != null ? Mathf.Abs(rt.sizeDelta.x * rt.sizeDelta.y) : 999999f;
                var score = area + (isCostNamed ? 0f : 1000f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            if (best != null)
            {
                openCostText = best;
            }
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
            _animationState.SetValueAndForceNotify(IsLocked
                        ? AnimationState.None
                        : AnimationState.Idle);
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
                    lockImage.SetActive(true && IsLockNameShow);
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
            EnsureOpenCostTextWired();
            if (openCostText != null)
            {
                var prevText = openCostText.text;
                var unlockRow = TableSheets.Instance.WorldUnlockSheet
                    .OrderedList
                    .FirstOrDefault(r => r.WorldIdToUnlock == Id);
                if (unlockRow == null)
                {
                    _openCost = 0;
                    openCostText.text = string.Empty;
                    return;
                }

                _openCost = CrystalCalculator
                    .CalculateWorldUnlockCost(new[] { Id }, TableSheets.Instance.WorldUnlockSheet)
                    .MajorUnit;
                openCostText.text = _openCost.ToString();
            }
        }

        // Used when we want to show a world button even if the WorldSheet row is missing.
        public void Set(int worldId, int stageBegin = 0, int stageEnd = 0)
        {
            Id = worldId;
            StageBegin = stageBegin;
            StageEnd = stageEnd;
            EnsureOpenCostTextWired();

            if (openCostText == null)
            {
                return;
            }

            var prevText = openCostText.text;
            var unlockRow = TableSheets.Instance.WorldUnlockSheet
                .OrderedList
                .FirstOrDefault(r => r.WorldIdToUnlock == worldId);

            if (unlockRow == null)
            {
                _openCost = 0;
                openCostText.text = string.Empty;
                return;
            }

            _openCost = CrystalCalculator
                .CalculateWorldUnlockCost(new[] { Id }, TableSheets.Instance.WorldUnlockSheet)
                .MajorUnit;
            openCostText.text = _openCost.ToCurrencyNotation();
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
