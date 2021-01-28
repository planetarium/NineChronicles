using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using RedBlueGames.Tools.TextTyper;

namespace Nekoyume.UI
{
    public class GuideDialog : TutorialItem
    {
        [SerializeField] private Transform topContainer;
        [SerializeField] private Transform bottomContainer;
        [SerializeField] private TextTyper textTyper;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private List<Emoji> emojiList;
        [SerializeField] private List<Comma> commaList;

        [SerializeField] private float fadeDuration = 1.0f;

        private string _script = string.Empty;

        private System.Action _callback;

        public override void Play<T>(T data, System.Action callback)
        {
            if (data is GuideDialogData d)
            {
                transform.SetParent(d.TargetHeight > 0 ? topContainer : bottomContainer);
                transform.localPosition = Vector3.zero;
                ShowEmoji(d.EmojiType);
                textTyper.TypeText(string.Empty);
                textTyper.PrintCompleted.RemoveAllListeners();
                textTyper.PrintCompleted.AddListener(() => { ShowComma(d.CommaType); });
                ShowComma(DialogCommaType.None);
                SetFade(true, fadeDuration, () =>
                {
                    _script = d.Script;
                    Typing();

                    d.Button.onClick.AddListener(OnClick);
                    _callback = callback;
                });
            }
        }

        public override void Stop()
        {
            SetFade(false, fadeDuration);
        }

        private void OnClick()
        {
            if (textTyper.IsSkippable())
            {
                textTyper.Skip();
            }
            else
            {
                _callback?.Invoke();
            }
        }

        private void ShowEmoji(DialogEmojiType type)
        {
            foreach (var emoji in emojiList)
            {
                emoji.Animation.SetActive(emoji.Type == type);
            }
        }

        private void ShowComma(DialogCommaType type)
        {
            foreach (var comma in commaList)
            {
                comma.Icon.SetActive(comma.Type == type);
            }
        }

        private void Typing()
        {
            if (_script.Equals(string.Empty))
            {
                return;
            }

            textTyper.TypeText(_script);
            _script = string.Empty;
        }

        private void SetFade(bool isIn, float duration, System.Action action = null)
        {
            canvasGroup.alpha = isIn ? 0 : 1;
            canvasGroup.DOFade(isIn ? 1 : 0, duration).OnComplete(() => action?.Invoke());
        }
    }

    [Serializable]
    public class Emoji
    {
        [SerializeField] private DialogEmojiType type;
        [SerializeField] private GameObject animtion;

        public DialogEmojiType Type => type;
        public GameObject Animation => animtion;
    }

    [Serializable]
    public class Comma
    {
        [SerializeField] private DialogCommaType type;
        [SerializeField] private GameObject icon;

        public DialogCommaType Type => type;
        public GameObject Icon => icon;
    }
}
