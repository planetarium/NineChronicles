using System;
using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventType = Nekoyume.EnumType.EventType;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class WorldMapStage : MonoBehaviour
    {
        public enum State
        {
            Normal,
            Disabled,
            Hidden
        }

        public class ViewModel : IDisposable
        {
            public readonly StageType stageType;
            public readonly int stageId;
            public readonly BossType bossType;
            public readonly EventType eventType;
            public readonly ReactiveProperty<State> State = new();
            public readonly ReactiveProperty<bool> Selected = new();
            public readonly ReactiveProperty<bool> HasNotification = new(false);

            public ViewModel(StageType stageType, State state)
                : this(stageType, -1, BossType.None, EventType.Default, state)
            {
            }

            public ViewModel(
                StageType stageType,
                int stageId,
                BossType bossType,
                EventType eventType,
                State state)
            {
                this.stageType = stageType;
                this.stageId = stageId;
                this.bossType = bossType;
                this.eventType = eventType;
                State.Value = state;
            }

            public void Dispose()
            {
                State.Dispose();
                Selected.Dispose();
            }
        }

        public float bossScale = 1f;

        [SerializeField]
        private Image normalImage;

        [SerializeField]
        private Image disabledImage;

        [SerializeField]
        private Image selectedImage;

        [SerializeField]
        private Image bossImage;

        [SerializeField]
        private Button button;

        [SerializeField]
        private TextMeshProUGUI buttonText;

        [SerializeField]
        private GameObject hasNotificationImage;

        private Vector3 _normalImageScale;

        private Vector3 _disabledImageScale;

        private Vector3 _selectedImageScale;

        private readonly List<IDisposable> _disposablesForModel = new();

        private Tweener _tweener;

        public readonly Subject<WorldMapStage> onClick = new();

        public ViewModel SharedViewModel { get; private set; }

        private void Awake()
        {
            _normalImageScale = normalImage.transform.localScale;
            _disabledImageScale = disabledImage.transform.localScale;
            _selectedImageScale = selectedImage.transform.localScale;

            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    onClick.OnNext(this);
                }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            SubscribeSelect(SharedViewModel?.Selected.Value ?? false);
        }

        private void OnDisable()
        {
            _tweener?.Kill();
            _tweener = null;
        }

        public void Show(ViewModel viewModel)
        {
            if (viewModel is null)
            {
                Hide();

                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            SharedViewModel = viewModel;
            SharedViewModel.State.Subscribe(SubscribeState).AddTo(_disposablesForModel);
            SharedViewModel.Selected.Subscribe(SubscribeSelect).AddTo(_disposablesForModel);
            SharedViewModel.HasNotification.SubscribeTo(hasNotificationImage).AddTo(_disposablesForModel);
            Set(SharedViewModel.bossType, SharedViewModel.eventType);

            buttonText.text = StageInformation.GetStageIdString(
                SharedViewModel.stageType,
                SharedViewModel.stageId);
        }

        public void Hide()
        {
            SharedViewModel.State.Value = State.Hidden;
        }

        private void SubscribeState(State value)
        {
            if (SharedViewModel?.Selected.Value ?? false)
            {
                return;
            }

            switch (value)
            {
                case State.Normal:
                    gameObject.SetActive(true);
                    normalImage.enabled = true;
                    disabledImage.enabled = false;
                    selectedImage.enabled = false;
                    buttonText.color = ColorHelper.HexToColorRGB("FFF9DD");
                    break;
                case State.Disabled:
                    gameObject.SetActive(true);
                    normalImage.enabled = false;
                    disabledImage.enabled = true;
                    selectedImage.enabled = false;
                    buttonText.color = ColorHelper.HexToColorRGB("666666");
                    break;
                case State.Hidden:
                    gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            if (L10nManager.TryGetFontMaterial(FontMaterialType.ButtonNormal, out var fontMaterial))
            {
                buttonText.fontSharedMaterial = fontMaterial;
            }

            normalImage.SetNativeSize();
        }

        private void SubscribeSelect(bool value)
        {
            _tweener?.Kill();
            _tweener = null;
            transform.localScale = Vector3.one;

            if (!value)
            {
                SubscribeState(SharedViewModel?.State.Value ?? State.Normal);
                return;
            }

            gameObject.SetActive(true);
            normalImage.enabled = false;
            disabledImage.enabled = false;
            selectedImage.enabled = true;
            buttonText.color = ColorHelper.HexToColorRGB("FFF9DD");

            if (L10nManager.TryGetFontMaterial(FontMaterialType.ButtonYellow, out var fontMaterial))
            {
                buttonText.fontSharedMaterial = fontMaterial;
            }

            _tweener = transform
                .DOScale(1.2f, 1f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void Set(BossType bossType, EventType eventType)
        {
            var stageIcon = WorldMapDataHelper.GetStageIcon(bossType, eventType);
            var icon = stageIcon.icon.sprite;
            var offset = stageIcon.icon.offset;
            var selectedColor = stageIcon.selectedColor;

            normalImage.sprite = icon;
            normalImage.SetNativeSize();
            normalImage.rectTransform.anchoredPosition = offset;

            disabledImage.sprite = icon;
            disabledImage.SetNativeSize();
            disabledImage.rectTransform.anchoredPosition = offset;

            selectedImage.sprite = icon;
            selectedImage.SetNativeSize();
            selectedImage.rectTransform.anchoredPosition = offset;
            selectedImage.color = selectedColor;

            var bossMark = WorldMapDataHelper.GetBossMarkIcon(bossType);
            var hasBossMark = bossMark != null;

            ResetScale();
            bossImage.enabled = hasBossMark;
            if (hasBossMark)
            {
                SetScale(bossScale);
                bossImage.sprite = bossMark.sprite;
                bossImage.SetNativeSize();
                bossImage.rectTransform.anchoredPosition = bossMark.offset;
            }
        }

        private void ResetScale()
        {
            normalImage.transform.localScale = _normalImageScale;
            disabledImage.transform.localScale = _disabledImageScale;
            selectedImage.transform.localScale = _selectedImageScale;
        }

        private void SetScale(float scale)
        {
            Vector3 scaleVector = Vector2.one * scale;
            normalImage.transform.localScale = scaleVector;
            disabledImage.transform.localScale = scaleVector;
            selectedImage.transform.localScale = scaleVector;
        }
    }
}
