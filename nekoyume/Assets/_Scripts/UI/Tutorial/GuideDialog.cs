using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.Game.Controller;
using UnityEngine;
using RedBlueGames.Tools.TextTyper;
using Nekoyume.L10n;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideDialog : TutorialItem
    {
        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] private Transform topContainer;
        [SerializeField] private Transform bottomContainer;
        [SerializeField] private TextTyper textTyper;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private List<Emoji> emojiList;
        [SerializeField] private List<Comma> commaList;

        private string _script = string.Empty;

        private System.Action _callback;
        private Coroutine _coroutine;
        private Button _button;

        public override void Play<T>(T data, System.Action callback)
        {
            if (data is GuideDialogData d)
            {
                if (_coroutine != null)
                {
                    StopCoroutine(_coroutine);
                }
                _coroutine = StartCoroutine(LatePlay(d, callback));
            }
        }

        public override void Stop(System.Action callback)
        {
            SetFade(false, fadeDuration, callback);
        }

        private IEnumerator LatePlay(GuideDialogData data, System.Action callback)
        {
            yield return new WaitForSeconds(predelay);
            transform.SetParent(data.TargetHeight > 0 ? topContainer : bottomContainer);
            transform.localPosition = Vector3.zero;
            ShowEmoji(data.EmojiType);
            PlaySound(data.EmojiType);
            textTyper.TypeText(string.Empty);
            textTyper.PrintCompleted.RemoveAllListeners();
            textTyper.PrintCompleted.AddListener(() => { ShowComma(data.CommaType); });
            textTyper.CharacterPrinted.RemoveAllListeners();
            textTyper.CharacterPrinted.AddListener(PlaySound);
            ShowComma(DialogCommaType.None);
            SetFade(true, fadeDuration, () =>
            {
                var l10nKey = data.ScriptL10nKey;
                _script = L10nManager.TryLocalize(l10nKey, out var script) ?
                    script : $"!!{data.ScriptL10nKey}";
                Typing();

                _button = data.Button;
                _button.onClick.AddListener(OnClick);
                _callback = callback;
            });
        }

        private void OnClick()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.Click);
            if (textTyper.IsSkippable())
            {
                textTyper.Skip();
            }
            else
            {
                _button?.onClick.RemoveAllListeners();
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

        private void PlaySound(DialogEmojiType type)
        {
            switch (type)
            {
                case DialogEmojiType.Idle:
                    AudioController.instance.PlaySfx(AudioController.SfxCode.NPC_Common);
                    break;
                case DialogEmojiType.Reaction:
                    AudioController.instance.PlaySfx(AudioController.SfxCode.NPC_Congrat);
                    break;
                case DialogEmojiType.Question:
                    AudioController.instance.PlaySfx(AudioController.SfxCode.NPC_Question);
                    break;
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

        private void SetFade(bool isIn, float duration, System.Action action)
        {
            canvasGroup.alpha = isIn ? 0 : 1;
            canvasGroup.DOFade(isIn ? 1 : 0, duration)
                .SetEase(fadeCurve)
                .OnComplete(() => action?.Invoke());
        }

        private void PlaySound(string printedCharacter)
        {
            if (printedCharacter == " " || printedCharacter == "\n")
            {
                return;
            }

            AudioController.instance.PlaySfx(AudioController.SfxCode.Typing, 0.1f);
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
