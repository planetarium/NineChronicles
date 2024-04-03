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
using UniRx.Toolkit;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;
using Nekoyume.L10n;
using System.Text.RegularExpressions;
using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    using UniRx;

    public class HelpTooltip : Widget
    {
        public override WidgetType WidgetType => WidgetType.Tooltip;

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
            [NonSerialized]
            public List<PageElement> elements;
        }

        [Serializable]
        public abstract class PageElement
        {
            public float delay;
            public float duration;
            public float2 anchoredPosition;
        }

        [Serializable]
        public class PageImageModel : PageElement
        {
            public string resourcePath;

            // NOTE: 아래 두 값이 없으면 해상도 변경에 따라서 포지션 이슈가 생길 수 있습니다.
            // public AnchorPresetType AnchorPresetType { get; }
            // public PivotPresetType PivotPresetType { get; }
            public bool spinning;
        }

        [Serializable]
        public class PageTextModel : PageElement
        {
            public string textL10nKey;
            public int fontSize;
            // Default value : TopLeft
            public string alignment;
        }

        #endregion

        private const string JsonDataPath = "HelpPopupData/HelpPopupData";

        private static HelpTooltip _instanceCache;
        private static List<ViewModel> _sharedViewModelsCache;

        private static HelpTooltip Instance => _instanceCache
            ? _instanceCache
            : _instanceCache = Find<HelpTooltip>();

        private static List<ViewModel> SharedViewModels =>
            _sharedViewModelsCache ?? (_sharedViewModelsCache = GetViewModels());

        [SerializeField]
        private Button backgroundImageButton = null;

        [SerializeField]
        private RectTransform panel = null;

        [SerializeField]
        private RectTransform textArea = null;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private SimpleUIImagePool imagePool = null;

        [SerializeField]
        private SimpleTextMeshProUGUIPool textPool = null;

        [SerializeField]
        private Button previousButton = null;

        [SerializeField]
        private Button nextButton = null;

        [SerializeField]
        private Button gotItButton = null;

        [SerializeField]
        private TransformLocalScaleTweener showingTweener = null;

        private ViewModel _viewModel;
        private int _pageIndex = -1;
        private int _pageElementIndex = -1;
        private float _timeSinceStartElement;
        private List<(Image, float)> _images = new List<(Image, float)>();
        private List<(Image, float)> _spinningImages = new List<(Image, float)>();
        private List<(TextMeshProUGUI, float)> _texts = new List<(TextMeshProUGUI, float)>();

        #region Control

        public static void HelpMe(int id, bool showOnceForEachAgentAddress = default)
        {
            if (showOnceForEachAgentAddress)
            {
                if (PlayerPrefs.HasKey(
                    $"{nameof(HelpTooltip)}_{id}_{States.Instance.AgentState.address}"))
                {
                    return;
                }

                PlayerPrefs.SetInt(
                    $"{nameof(HelpTooltip)}_{id}_{States.Instance.AgentState.address}",
                    1);
            }

            if (!Instance.TrySetId(id))
            {
                return;
            }
            var props = new Dictionary<string, Value>()
            {
                ["HelpPopupId"] = id,
            };

            Analyzer.Instance.Track("Unity/Click HelpPopup", props);

            var evt = new AirbridgeEvent("Click_Help_Popup");
            evt.SetValue(id);
            AirbridgeUnity.TrackEvent(evt);

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
                foreach (var view in jsonModel.viewModels)
                {
                    foreach (var page in view.pages)
                    {
                        page.elements = new List<PageElement>();
                        foreach (var image in page.images)
                        {
                            page.elements.Add(image);
                        }

                        foreach (var text in page.texts)
                        {
                            page.elements.Add(text);
                        }
                        page.elements.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
                    }
                }
                return jsonModel.viewModels.ToList();
            }

            var sb = new StringBuilder($"[{nameof(HelpTooltip)}]");
            sb.Append($" {nameof(GetViewModels)}()");
            sb.Append($" Failed to load resource at {JsonDataPath}");
            NcDebug.LogError(sb.ToString());
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
            previousButton.OnClickAsObservable()
                .ThrottleFirst(throttleDuration)
                .Subscribe(OnClickPrevious)
                .AddTo(gameObject);
            nextButton.OnClickAsObservable()
                .ThrottleFirst(throttleDuration)
                .Subscribe(OnClickNext)
                .AddTo(gameObject);
            gotItButton.OnClickAsObservable()
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
                NcDebug.LogError("Failed to initialize.");
            }
        }

        protected override void Update()
        {
            base.Update();
            _timeSinceStartElement += Time.deltaTime;
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

            // Elements
            CheckTimeAndReturnToPool(ref _images, imagePool);
            CheckTimeAndReturnToPool(ref _texts, textPool);
            UpdateSpinningImages();
            TryShowNextElement(page);
        }

        private void UpdateButtons()
        {
            nextButton.gameObject.SetActive(false);
            previousButton.gameObject.SetActive(false);
            gotItButton.gameObject.SetActive(false);

            if (_viewModel is null ||
                _pageIndex < 0 ||
                _pageIndex >= _viewModel.pages.Length)
            {
                return;
            }

            if (_pageIndex > 0)
            {
                previousButton.gameObject.SetActive(true);
            }

            if (_pageIndex < _viewModel.pages.Length - 1)
            {
                nextButton.gameObject.SetActive(true);
            }
            else
            {
                gotItButton.gameObject.SetActive(true);
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
                var sb = new StringBuilder($"[{nameof(HelpTooltip)}]");
                sb.Append($" {nameof(TrySetId)}({id.GetType().Name} {nameof(id)}):");
                sb.Append($" Cannot found {id}");
                NcDebug.LogError(sb.ToString());
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
            _pageElementIndex = -1;
            _timeSinceStartElement = 0f;

            var pageModel = viewModel.pages[_pageIndex];
            titleText.text = L10nManager.Localize(pageModel.titleL10nKey);

            UpdatePageImagesAndTexts();
            UpdateButtons();
            return true;
        }

        private bool TryShowNextElement(PageModel pageModel)
        {
            var pageElementIndex = _pageElementIndex + 1;
            if (pageElementIndex < 0 ||
                pageElementIndex >= pageModel.elements.Count)
            {
                return false;
            }

            var element = pageModel.elements[pageElementIndex];
            // When image and text appears at the same time.
            if (element.delay + Time.deltaTime < _timeSinceStartElement)
            {
                return false;
            }

            switch (element)
            {
                case PageImageModel imageModel:
                    ShowImage(imageModel, pageElementIndex);
                    break;
                case PageTextModel textModel:
                    var nextElement = (pageModel.elements.Count > pageElementIndex + 1) ?
                        pageModel.elements[pageElementIndex + 1] : null;
                    var nextElementPosY =
                        nextElement is null ? -textArea.rect.height : nextElement.anchoredPosition.y;
                    ShowText(textModel, pageElementIndex, nextElementPosY);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private void ShowImage(PageImageModel imageModel, int index)
        {
            var sprite = Resources.Load<Sprite>(imageModel.resourcePath);
            if (sprite is null)
            {
                NcDebug.LogError($"Failed to load resource at {imageModel.resourcePath}");
                return;
            }

            _pageElementIndex = index;
            _timeSinceStartElement = 0f;

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
                NcDebug.LogWarning("_spinningImages.Add() called!");
            }
        }

        private void ShowText(PageTextModel textModel, int index, float nextElementPosY)
        {
            _pageElementIndex = index;
            _timeSinceStartElement = 0f;

            var text = textPool.Rent();
            var textRectTransform = text.rectTransform;
            textRectTransform.anchoredPosition = textModel.anchoredPosition;
            textRectTransform.sizeDelta =
                new Vector2(textRectTransform.sizeDelta.x,
                Mathf.Abs(nextElementPosY - textRectTransform.anchoredPosition.y));
            var localizedString = L10nManager.Localize(textModel.textL10nKey);
            text.text = Regex.Unescape(localizedString);
            text.fontSizeMax = textModel.fontSize;

            if (!Enum.TryParse<TextAlignmentOptions>(textModel.alignment, out var alignment))
            {
                text.alignment = TextAlignmentOptions.TopLeft;
            }
            else
            {
                text.alignment = alignment;
            }

            _texts.Add((
                text,
                textModel.duration < 0f
                    ? -1f
                    : Time.timeSinceLevelLoad + textModel.duration));
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

        private void OnClickPrevious(Unit unit)
        {
            TrySetPage(_pageIndex - 1);
        }

        private void OnClickNext(Unit unit)
        {
            TrySetPage(_pageIndex + 1);
        }

        private static void OnClickGotIt(Unit unit)
        {
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
            var elementsCount = page.elements.Count;
            if (_pageElementIndex < elementsCount - 1 ||
                _pageElementIndex >= elementsCount)
            {
                return false;
            }

            return true;
        }
    }
}
