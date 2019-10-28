using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class QuestCellView : EnhancedScrollerCellView
    {
        public Action<QuestCellView> onClickSubmitButton;

        private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("001870");
        public TextMeshProUGUI content;
        public Text buttonText;
        public Button button;
        public Game.Quest.Quest data;

        public IDisposable onClickDisposable;
        private Shadow[] _textShadows;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
            onClickDisposable = button.OnClickAsObservable()
                .Subscribe(_ => onClickSubmitButton?.Invoke(this))
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            onClickDisposable?.Dispose();
            button.interactable = true;
        }

        #endregion

        public void SetData(Game.Quest.Quest quest)
        {
            _textShadows = button.GetComponentsInChildren<Shadow>();
            data = quest;
            var text = quest.ToInfo();
            var color = quest.Complete ? ColorHelper.HexToColorRGB("7a7a7a") : ColorHelper.HexToColorRGB("fff9dd");
            content.text = text;
            content.color = color;

            buttonText.text = LocalizationManager.Localize("UI_GET_REWARD");
            button.interactable = quest.Complete;
            foreach (var shadow in _textShadows)
                shadow.effectColor = button.interactable ? _highlightedColor : Color.black;

        }

        public void RequestReward()
        {
            button.interactable = false;
            foreach (var shadow in _textShadows)
                shadow.effectColor = Color.black;
        }
    }
}
