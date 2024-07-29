using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class Tutorial : Widget
    {
        public override WidgetType WidgetType => WidgetType.TutorialMask;

        [SerializeField]
        private Button button;

        [SerializeField]
        private List<ItemContainer> items;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private float playTime = 2;

        [SerializeField]
        private GuideDialog guideDialog;

        [SerializeField]
        private Image guideImage;

        [SerializeField]
        private Sprite transparentSprite;

        [SerializeField]
        private Button skipButton;

        private Coroutine _coroutine;
        private System.Action _callback;
        private const int ItemCount = 3;
        private int _playTimeRef;
        private int _finishRef;
        private bool _isPlaying;
        private IDisposable _onClickDispose = null;
        private IDisposable _onClickWithSkipDispose = null;
        public BattleTutorialController TutorialController { get; private set; }

        public Button NextButton => button;

        public override void Initialize()
        {
            base.Initialize();
            TutorialController = new BattleTutorialController();
            skipButton.onClick.AddListener(SkipSession);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            guideDialog.gameObject.SetActive(false);
            items.ForEach(item => item.Item.gameObject.SetActive(false));
            guideImage.sprite = transparentSprite;
            base.Close(ignoreCloseAnimation);
        }

        public void Play(List<ITutorialData> datas, int presetId, Sprite guideSprite = null, System.Action callback = null)
        {
            if (!(_onClickDispose is null))
            {
                _onClickDispose.Dispose();
                _onClickDispose = null;
            }

            if (!(_onClickWithSkipDispose is null))
            {
                _onClickWithSkipDispose.Dispose();
                _onClickWithSkipDispose = null;
            }

            _onClickDispose = button.OnClickAsObservable()
                .Where(_ => !_isPlaying)
                .Subscribe(_ => OnClick())
                .AddTo(gameObject);

            _onClickWithSkipDispose = Observable.EveryUpdate()
                .Where(_ => _isPlaying && Input.GetMouseButtonUp(0))
                .Subscribe(_ =>
                {
                    foreach (var item in items)
                    {
                        item.Item.Skip(() => PlayEnd());
                    }
                })
                .AddTo(gameObject);

            if (_isPlaying)
            {
                return;
            }

            const int skippableTutorialId = 50011;
            var isSkippable = Game.Game.instance.Stage.TutorialController.LastPlayedTutorialId >
                skippableTutorialId;
            skipButton.gameObject.SetActive(isSkippable);
            _finishRef = 0;
            _isPlaying = true;

            animator.SetTrigger(presetId.ToString());
            RunStopwatch();
            foreach (var data in datas)
            {
                var item = items.FirstOrDefault(x => data.Type == x.Type);
                item?.Item.gameObject.SetActive(true);
                item?.Item.Play(data, () => PlayEnd());
            }

            guideImage.sprite = guideSprite != null ? guideSprite : transparentSprite;
            _callback = callback;
        }

        public void PlaySmallGuide(int id)
        {
            void ShowGuideDialog(BattleTutorialController.BattleTutorialModel model)
            {
                Show();
                guideDialog.Show(model, () =>
                {
                    if (model.NextId == 0)
                    {
                        Close(true);
                    }
                    else
                    {
                        ShowGuideDialog(TutorialController.GetBattleTutorialModel(model.NextId));
                    }
                });
            }

            if (TutorialController.TryGetBattleTutorialModel(id, out var model))
            {
                guideDialog.gameObject.SetActive(false);
                ShowGuideDialog(model);
            }
        }

        public void PlayOnlyGuideArrow(GuideType guideType,
            RectTransform target,
            Vector2 targetPositionOffset = default,
            Vector2 targetSizeOffset = default,
            Vector2 arrowPositionOffset = default)
        {
            Show(true);
            var arrow = items.First(i => i.Type == TutorialItemType.Arrow).Item;
            arrow.gameObject.SetActive(true);
            arrow.Play(new GuideArrowData(
                    guideType,
                    target,
                    targetPositionOffset,
                    targetSizeOffset,
                    arrowPositionOffset,
                    0f,
                    false),
                null);
        }

        public void Stop(System.Action callback = null)
        {
            _onClickDispose?.Dispose();
            _onClickDispose = null;
            _onClickWithSkipDispose?.Dispose();
            _onClickWithSkipDispose = null;
            _finishRef = 0;
            _playTimeRef = 0;
            _isPlaying = true;
            guideImage.sprite = transparentSprite;
            skipButton.gameObject.SetActive(false);
            foreach (var item in items)
            {
                item.Item.Stop(() => PlayEnd(callback));
            }
        }

        private void SkipSession()
        {
            var controller = Game.Game.instance.Stage.TutorialController;
            controller.Skip(controller.LastPlayedTutorialId);
        }

        private void RunStopwatch()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }

            _coroutine = StartCoroutine(Stopwatch());
        }

        private IEnumerator Stopwatch()
        {
            _playTimeRef = 1;
            yield return new WaitForSeconds(playTime);
            PlayEnd();
        }

        private void PlayEnd(System.Action callback = null)
        {
            _finishRef++;
            if (_finishRef >= ItemCount + _playTimeRef)
            {
                _isPlaying = false;
                callback?.Invoke();
            }
        }

        private void OnClick()
        {
            if (_isPlaying)
            {
                return;
            }

            AudioController.instance.PlaySfx(AudioController.SfxCode.Click);
            _callback?.Invoke();
        }
    }

    [Serializable]
    public class ItemContainer
    {
        [SerializeField] private TutorialItemType type;
        [SerializeField] private TutorialItem item;

        public TutorialItemType Type => type;
        public TutorialItem Item => item;
    }
}
