using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HelpPopup : PopupWidget
    {
        #region Models

        private class ViewModel
        {
            public int2 Size { get; set; }
            public List<PageModel> Pages { get; set; }
        }

        private class PageModel
        {
            public string TitleL10nKey { get; set; }
            public List<PageSpriteModel> Sprites { get; set; }
            public List<PageTextModel> Texts { get; set; }
        }

        private class PageSpriteModel
        {
            public float Delay { get; set;}
            public float Duration { get; set; }

            public string ResourcePath { get; set; }

            // NOTE: 아래 두 값이 없으면 해상도 변경에 따라서 포지션 이슈가 생길 수 있습니다.
            // public AnchorPresetType AnchorPresetType { get; }
            // public PivotPresetType PivotPresetType { get; }
            public int2 LocalPosition { get; set; }
        }

        private class PageTextModel
        {
            public float Delay { get; set; }
            public float Duration { get; set; }
            public string TextL10nKey { get; set; }
            public int FontSize { get; set; }
            public int2 LocalPosition { get; set; }
        }

        #endregion

        private static HelpPopup _instanceCache;
        private static Dictionary<int, ViewModel> _sharedViewModelsCache;

        private static HelpPopup Instance => _instanceCache
            ? _instanceCache
            : _instanceCache = Find<HelpPopup>();

        private static Dictionary<int, ViewModel> SharedViewModels =>
            _sharedViewModelsCache ?? (_sharedViewModelsCache = GetViewModels());

        [SerializeField]
        private Button nextButton = null;

        [SerializeField]
        private Button previousButton = null;

        [SerializeField]
        private Button gotItButton = null;

        private int _id;
        private ViewModel _viewModel;
        private int _pageIndex;
        private int _pageSpriteIndex;
        private int _pageTextIndex;
        private float _timeSinceStartSprite;
        private float _timeSinceStartText;

        public static void Help(int id)
        {
            if (Instance.IsActive())
            {
                Instance.Close(true);
            }

            if (Instance.TrySetId(id))
            {
                Instance.Show();
            }
        }

        private static Dictionary<int, ViewModel> GetViewModels()
        {
            return null;
        }

        protected override void Awake()
        {
            base.Awake();

            nextButton.OnClickAsObservable().Subscribe(Next).AddTo(gameObject);
            previousButton.OnClickAsObservable().Subscribe(Previous).AddTo(gameObject);
            gotItButton.OnClickAsObservable().Subscribe(GotIt).AddTo(gameObject);
        }

        protected override void Update()
        {
            base.Update();
            _timeSinceStartSprite += Time.deltaTime;
            _timeSinceStartText += Time.deltaTime;
            UpdatePageSpritesAndTexts();
        }

        private void UpdatePageSpritesAndTexts()
        {
            if (_viewModel is null)
            {
                return;
            }

            // Pages
            var page = _viewModel.Pages[_pageIndex];

            // Sprites
            var sprite = page.Sprites[_pageSpriteIndex];
            if (sprite.Delay >= _timeSinceStartSprite &&
                TrySetPageSprite(page, _pageSpriteIndex + 1))
            {
                _timeSinceStartSprite = 0f;
            }

            // Texts
            var text = page.Texts[_pageTextIndex];
            if (text.Delay >= _timeSinceStartText &&
                TrySetPageText(page, _pageTextIndex + 1))
            {
                _timeSinceStartText = 0f;
            }
        }

        private bool TrySetId(int id)
        {
            if (id == _id)
            {
                return true;
            }

            try
            {
                var viewModel = SharedViewModels.First(pair => pair.Key == _id).Value;
                _id = id;
                _viewModel = viewModel;
                TrySetPage(_viewModel, 0);
                return true;
            }
            catch (Exception)
            {
                var sb = new StringBuilder($"[{nameof(HelpPopup)}]");
                sb.Append($" {nameof(TrySetId)}({id.GetType().Name} {nameof(id)}):");
                sb.Append($" Cannot found {id}");
                Debug.LogError(sb.ToString());
                return false;
            }
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
                pageIndex >= viewModel.Pages.Count)
            {
                return false;
            }

            _pageIndex = pageIndex;
            var pageMode = viewModel.Pages[_pageIndex];
            TrySetPageSprite(pageMode, 0);
            TrySetPageText(pageMode, 0);
            return true;
        }

        private bool TrySetPageSprite(PageModel pageModel, int pageSpriteIndex)
        {
            if (pageSpriteIndex < 0 ||
                pageSpriteIndex >= pageModel.Sprites.Count)
            {
                return false;
            }

            _pageSpriteIndex = pageSpriteIndex;
            _timeSinceStartSprite = 0f;
            return true;
        }

        private bool TrySetPageText(PageModel pageModel, int pageTextIndex)
        {
            if (pageTextIndex < 0 ||
                pageTextIndex >= pageModel.Sprites.Count)
            {
                return false;
            }

            _pageTextIndex = pageTextIndex;
            _timeSinceStartText = 0f;
            return true;
        }

        private void Next(Unit unit)
        {
            TrySetPage(_pageIndex + 1);
        }

        private void Previous(Unit unit)
        {
            TrySetPage(_pageIndex - 1);
        }

        private void GotIt(Unit unit)
        {
            Close();
        }
    }
}
