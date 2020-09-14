using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Pool;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using TMPro;
using UniRx;
using UniRx.Toolkit;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;
using Nekoyume.L10n;

namespace Nekoyume.UI
{
    public class HelpPopup : PopupWidget
    {
        #region Models

        [Serializable]
        public class JsonModel
        {
            public ViewModel[] viewModels;
        }

        [Serializable]
        public class ViewModel
        {
            public int id;
            public float2 size;
            public PageModel[] pages;
        }

        [Serializable]
        public class PageModel
        {
            public string titleL10nKey;
            public PageImageModel[] images;
            public PageTextModel[] texts;
        }

        [Serializable]
        public class PageImageModel
        {
            public float delay;
            public float duration;
            public string resourcePath;

            // NOTE: 아래 두 값이 없으면 해상도 변경에 따라서 포지션 이슈가 생길 수 있습니다.
            // public AnchorPresetType AnchorPresetType { get; }
            // public PivotPresetType PivotPresetType { get; }
            public float2 anchoredPosition;
            public bool spinning;
        }

        [Serializable]
        public class PageTextModel
        {
            public float delay;
            public float duration;
            public string textL10nKey;
            public int fontSize;
            public float2 anchoredPosition;
        }

        #endregion

        private const string JsonDataPath = "HelpPopupData/HelpPopupData";

        private static HelpPopup _instanceCache;
        private static List<ViewModel> _sharedViewModelsCache;

        private static HelpPopup Instance => _instanceCache
            ? _instanceCache
            : _instanceCache = Find<HelpPopup>();

        private static List<ViewModel> SharedViewModels =>
            _sharedViewModelsCache ?? (_sharedViewModelsCache = GetViewModels());

        [SerializeField]
        private Button backgroundImageButton = null;

        [SerializeField]
        private RectTransform panel = null;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private SimpleUIImagePool imagePool = null;

        [SerializeField]
        private SimpleTextMeshProUGUIPool textPool = null;

        [SerializeField]
        private SubmitButton previousButton = null;

        [SerializeField]
        private SubmitButton nextButton = null;

        [SerializeField]
        private SubmitButton gotItButton = null;

        [SerializeField]
        private TransformLocalScaleTweener showingTweener = null;

        private ViewModel _viewModel;
        private int _pageIndex = -1;
        private int _pageImageIndex = -1;
        private int _pageTextIndex = -1;
        private float _timeSinceStartImage;
        private float _timeSinceStartText;
        private List<(Image, float)> _images = new List<(Image, float)>();
        private List<(Image, float)> _spinningImages = new List<(Image, float)>();
        private List<(TextMeshProUGUI, float)> _texts = new List<(TextMeshProUGUI, float)>();

        #region Control

        public static void HelpMe(int id, bool showOnceForEachAgentAddress = default)
        {
            if (showOnceForEachAgentAddress)
            {
                if (PlayerPrefs.HasKey(
                    $"{nameof(HelpPopup)}_{id}_{States.Instance.AgentState.address}"))
                {
                    return;
                }

                PlayerPrefs.SetInt(
                    $"{nameof(HelpPopup)}_{id}_{States.Instance.AgentState.address}",
                    1);
            }

            if (!Instance.TrySetId(id))
            {
                return;
            }
            var props = new Value
            {
                ["HelpPopupId"] = id,
            };
            Mixpanel.Track("Unity/Click HelpPopup", props);

#pragma warning disable 618
            Instance.Show();
#pragma warning restore 618
        }

        public static void ThankYou()
        {
            if (!Instance.IsActive())
            {
                return;
            }

#pragma warning disable 618
            Instance.Close();
#pragma warning restore 618
        }

        #endregion

        private static List<ViewModel> GetViewModels()
        {
            var json = Resources.Load<TextAsset>(JsonDataPath)?.text;
            if (!string.IsNullOrEmpty(json))
            {
                var jsonModel = JsonUtility.FromJson<JsonModel>(json);
                return jsonModel.viewModels.ToList();
            }

            var sb = new StringBuilder($"[{nameof(HelpPopup)}]");
            sb.Append($" {nameof(GetViewModels)}()");
            sb.Append($" Failed to load resource at {JsonDataPath}");
            Debug.LogError(sb.ToString());
            return null;
        }

        protected override void Awake()
        {
            base.Awake();

            var throttleDuration = new TimeSpan(0, 0, 1);
            backgroundImageButton.onClick
                .AsObservable()
                .ThrottleFirst(throttleDuration)
                .Subscribe(OnClickBackground)
                .AddTo(gameObject);
            previousButton.OnSubmitClick
                .ThrottleFirst(throttleDuration)
                .Subscribe(OnClickPrevious)
                .AddTo(gameObject);
            nextButton.OnSubmitClick
                .ThrottleFirst(throttleDuration)
                .Subscribe(OnClickNext)
                .AddTo(gameObject);
            gotItButton.OnSubmitClick
                .ThrottleFirst(throttleDuration)
                .Subscribe(OnClickGotIt)
                .AddTo(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Instance is null ||
                SharedViewModels is null)
            {
                Debug.LogError("Failed to initialize.");
            }
        }

        protected override void Update()
        {
            base.Update();
            _timeSinceStartImage += Time.deltaTime;
            _timeSinceStartText += Time.deltaTime;
            UpdatePageImagesAndTexts();
        }

        private void UpdatePageImagesAndTexts()
        {
            if (_viewModel is null)
            {
                return;
            }

            // Pages
            var page = _viewModel.pages[_pageIndex];

            // Images
            CheckTimeAndReturnToPool(ref _images, imagePool);
            UpdateSpinningImages();
            TryShowNextImage(page);

            // Texts
            CheckTimeAndReturnToPool(ref _texts, textPool);
            TryShowNextText(page);
        }

        private void UpdateButtons()
        {
            nextButton.Hide();
            previousButton.Hide();
            gotItButton.Hide();

            if (_viewModel is null ||
                _pageIndex < 0 ||
                _pageIndex >= _viewModel.pages.Length)
            {
                return;
            }

            if (_pageIndex > 0)
            {
                previousButton.Show();
            }

            if (_pageIndex < _viewModel.pages.Length - 1)
            {
                nextButton.Show();
            }
            else
            {
                gotItButton.Show();
            }
        }

        private static void CheckTimeAndReturnToPool<T>(ref List<(T, float)> objects,
            ObjectPool<T> pool)
            where T : Component
        {
            objects = objects
                .Where(tuple =>
                {
                    var (obj, timeToReturn) = tuple;
                    if (timeToReturn < 0f ||
                        timeToReturn > Time.timeSinceLevelLoad)
                    {
                        return true;
                    }

                    pool.Return(obj);
                    return false;
                })
                .ToList();
        }

        private void UpdateSpinningImages()
        {
            _spinningImages = _spinningImages.Where(tuple =>
            {
                var (_, timeToReturn) = tuple;
                return timeToReturn < 0f ||
                       timeToReturn > Time.timeSinceLevelLoad;
            }).ToList();

            foreach (var (image, _) in _spinningImages)
            {
                image.rectTransform.Rotate(Vector3.forward, 180 * Time.deltaTime);
            }
        }

        #region Widget

        #pragma warning disable 0809
        [Obsolete("이 메서드 대신 HelpPopup.HelpMe(int id)를 사용합니다.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            if (ignoreShowAnimation)
            {
                showingTweener.PlayTween(0f).OnStart(() => base.Show(true));
            }
            else
            {
                showingTweener.PlayTween().OnStart(() => base.Show());
            }
        }

        [Obsolete("이 메서드 대신 HelpPopup.ThankYou()를 사용합니다.")]
        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (ignoreCloseAnimation)
            {
                showingTweener.PlayReverse(0f).OnComplete(() => base.Close());
            }
            else
            {
                showingTweener.PlayReverse().OnComplete(() => base.Close());
            }
        }
        #pragma warning restore 0809

        #endregion

        #region Try set or add

        private bool TrySetId(int id)
        {
            if (!(_viewModel is null) &&
                id == _viewModel.id)
            {
                return TrySetPage(0);
            }

            _viewModel = SharedViewModels.FirstOrDefault(e => e.id == id);
            if (_viewModel is null)
            {
                var sb = new StringBuilder($"[{nameof(HelpPopup)}]");
                sb.Append($" {nameof(TrySetId)}({id.GetType().Name} {nameof(id)}):");
                sb.Append($" Cannot found {id}");
                Debug.LogError(sb.ToString());
                return false;
            }

            ReturnToPoolAll();
            _pageIndex = -1;
            panel.sizeDelta = _viewModel.size;
            return TrySetPage(_viewModel, 0);
        }

        private bool TrySetPage(int pageIndex)
        {
            if (_viewModel is null)
            {
                return false;
            }

            return TrySetPage(_viewModel, pageIndex);
        }

        private bool TrySetPage(ViewModel viewModel, int pageIndex)
        {
            if (pageIndex < 0 ||
                pageIndex >= viewModel.pages.Length)
            {
                return false;
            }

            ReturnToPoolAll();
            _pageIndex = pageIndex;
            _pageImageIndex = -1;
            _pageTextIndex = -1;
            _timeSinceStartImage = 0f;
            _timeSinceStartText = 0f;

            var pageModel = viewModel.pages[_pageIndex];
            titleText.text = L10nManager.Localize(pageModel.titleL10nKey);

            UpdatePageImagesAndTexts();
            UpdateButtons();
            return true;
        }

        private bool TryShowNextImage(PageModel pageModel)
        {
            var pageImageIndex = _pageImageIndex + 1;
            if (pageImageIndex < 0 ||
                pageImageIndex >= pageModel.images.Length)
            {
                return false;
            }

            var imageModel = pageModel.images[pageImageIndex];
            if (imageModel.delay < _timeSinceStartImage)
            {
                return false;
            }

            var sprite = Resources.Load<Sprite>(imageModel.resourcePath);
            if (sprite is null)
            {
                Debug.LogError($"Failed to load resource at {imageModel.resourcePath}");
                return false;
            }

            _pageImageIndex = pageImageIndex;
            _timeSinceStartImage = 0f;

            var image = imagePool.Rent();
            var imageRectTransform = image.rectTransform;
            imageRectTransform.anchoredPosition = imageModel.anchoredPosition;
            image.overrideSprite = sprite;
            image.SetNativeSize();

            var tuple = (
                image,
                imageModel.duration < 0f
                    ? -1f
                    : Time.timeSinceLevelLoad + imageModel.duration);
            _images.Add(tuple);

            if (imageModel.spinning)
            {
                _spinningImages.Add(tuple);
                Debug.LogWarning("_spinningImages.Add() called!");
            }

            return true;
        }

        private bool TryShowNextText(PageModel pageModel)
        {
            var pageTextIndex = _pageTextIndex + 1;
            if (pageTextIndex < 0 ||
                pageTextIndex >= pageModel.texts.Length)
            {
                return false;
            }

            var textModel = pageModel.texts[pageTextIndex];
            if (textModel.delay < _timeSinceStartText)
            {
                return false;
            }

            _pageTextIndex = pageTextIndex;
            _timeSinceStartText = 0f;

            var text = textPool.Rent();
            text.rectTransform.anchoredPosition = textModel.anchoredPosition;
            text.text = L10nManager.Localize(textModel.textL10nKey);
            text.fontSize = textModel.fontSize;
            _texts.Add((
                text,
                textModel.duration < 0f
                    ? -1f
                    : Time.timeSinceLevelLoad + textModel.duration));

            return true;
        }

        #endregion

        private void ReturnToPoolAll()
        {
            foreach (var (image, _) in _images)
            {
                imagePool.Return(image);
            }

            _images.Clear();

            foreach (var (text, _) in _texts)
            {
                textPool.Return(text);
            }

            _texts.Clear();
        }

        private void OnClickBackground(Unit unit)
        {
            AudioController.PlayClick();
            ThankYou();
        }

        private void OnClickPrevious(SubmitButton button)
        {
            AudioController.PlayClick();
            TrySetPage(_pageIndex - 1);
        }

        private void OnClickNext(SubmitButton button)
        {
            AudioController.PlayClick();
            TrySetPage(_pageIndex + 1);
        }

        private static void OnClickGotIt(SubmitButton button)
        {
            AudioController.PlayClick();
            ThankYou();
        }

        private bool IsEndOfCurrentPage()
        {
            if (_viewModel is null)
            {
                return false;
            }

            var pagesLength = _viewModel.pages.Length;

            if (_pageIndex < pagesLength - 1 ||
                _pageIndex >= pagesLength)
            {
                return false;
            }

            var page = _viewModel.pages[_pageIndex];
            var imagesLength = page.images.Length;
            var textsLength = page.texts.Length;
            if (_pageImageIndex < imagesLength - 1 ||
                _pageImageIndex >= imagesLength ||
                _pageTextIndex < textsLength - 1 ||
                _pageTextIndex >= textsLength)
            {
                return false;
            }

            return true;
        }
    }
}
