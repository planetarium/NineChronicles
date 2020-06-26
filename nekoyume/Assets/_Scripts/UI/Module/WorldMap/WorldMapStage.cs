using System;
using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
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
            public readonly int stageId;
            public readonly string stageNumber;
            public readonly bool hasBoss;
            public readonly ReactiveProperty<State> State = new ReactiveProperty<State>();
            public readonly ReactiveProperty<bool> Selected = new ReactiveProperty<bool>();
            public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

            public ViewModel(StageWaveSheet.Row stageRow, string stageNumber, State state) :
                this(stageRow.StageId, stageNumber, stageRow.HasBoss, state)
            {
            }

            public ViewModel(State state) : this(-1, "0", false, state)
            {
            }

            public ViewModel(int stageId, string stageNumber, bool hasBoss, State state)
            {
                this.stageId = stageId;
                this.stageNumber = stageNumber;
                this.hasBoss = hasBoss;
                this.State.Value = state;
            }

            public void Dispose()
            {
                State.Dispose();
                Selected.Dispose();
            }
        }

        public float bossScale = 1.4f;

        [SerializeField]
        private Image normalImage = null;

        [SerializeField]
        private Image disabledImage = null;

        [SerializeField]
        private Image selectedImage = null;

        [SerializeField]
        private Image bossImage = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private TextMeshProUGUI buttonText = null;

        [SerializeField]
        private Image hasNotificationImage = null;

        private Vector3 _normalImageScale;
        private Vector3 _disabledImageScale;
        private Vector3 _selectedImageScale;
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        private Tweener _tweener;

        public readonly Subject<WorldMapStage> onClick = new Subject<WorldMapStage>();

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

            SetBoss(SharedViewModel.hasBoss);
            buttonText.text = SharedViewModel.stageNumber;
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
                    break;
                case State.Disabled:
                    gameObject.SetActive(true);
                    normalImage.enabled = false;
                    disabledImage.enabled = true;
                    selectedImage.enabled = false;
                    break;
                case State.Hidden:
                    gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
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

            _tweener = transform
                .DOScale(1.2f, 1f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void SetBoss(bool isBoss)
        {
            bossImage.enabled = isBoss;
            if (isBoss)
            {
                normalImage.transform.localScale *= bossScale;
                disabledImage.transform.localScale *= bossScale;
                selectedImage.transform.localScale *= bossScale;
            }
            else
            {
                normalImage.transform.localScale = _normalImageScale;
                disabledImage.transform.localScale = _disabledImageScale;
                selectedImage.transform.localScale = _selectedImageScale;
            }
        }
    }
}
