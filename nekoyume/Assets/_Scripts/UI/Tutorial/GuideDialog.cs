using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using UnityEngine;
using RedBlueGames.Tools.TextTyper;
using System;

namespace Nekoyume.UI
{
    public class GuideDialog : TutorialItem
    {
        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private AnimationCurve fadeCurve
            = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [SerializeField] private List<PrintDelay> printDelays = new List<PrintDelay>();
        [SerializeField] private Transform topContainer;
        [SerializeField] private Transform bottomContainer;
        [SerializeField] private TextTyper textTyper;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private List<Emoji> emojiList;
        [SerializeField] private List<Comma> commaList;

        private string _script = string.Empty;
        private Coroutine _coroutine;

        private readonly int PlayHash = Animator.StringToHash("Play");
        private readonly int StopHash = Animator.StringToHash("Stop");

        private const float DefaultPrintDelay = 0.02f;

        public override void Play<T>(T data, System.Action callback)
        {
            if (data is GuideDialogData d)
            {
                ShowComma(DialogCommaType.None);
                textTyper.TypeText(string.Empty);
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
            var height = data.target ? data.target.anchoredPosition.y : 0;
            transform.SetParent(height < 0 ? topContainer : bottomContainer);
            transform.localPosition = Vector3.zero;
            ShowEmoji(data.emojiType);
            PlaySound(data.emojiType);
            textTyper.PrintCompleted.AddListener(() => { OnPrintCompleted(data.commaType, callback); });
            textTyper.CharacterPrinted.AddListener(PlaySound);
            SetFade(true, fadeDuration, () =>
            {
                _script = data.script;
                PlayEmojiAnimation(PlayHash);
                Typing();
            });
        }

        private void OnPrintCompleted(DialogCommaType commaType, System.Action callback)
        {
            ShowComma(commaType);
            PlayEmojiAnimation(StopHash);
            callback?.Invoke();
            textTyper.PrintCompleted.RemoveAllListeners();
            textTyper.CharacterPrinted.RemoveAllListeners();
        }

        private void ShowEmoji(DialogEmojiType type)
        {
            foreach (var emoji in emojiList)
            {
                emoji.Animation.SetActive(emoji.Type == type);
            }
        }

        private void PlayEmojiAnimation(int hash)
        {
            foreach (var emoji in emojiList.Where(emoji => emoji.Animation.activeSelf))
            {
                emoji.Animation.GetComponent<Animator>().SetTrigger(hash);
                return;
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

            var printDelay = printDelays.FirstOrDefault(x => x.languageType == L10nManager.CurrentLanguage);
            textTyper.TypeText(_script, printDelay?.delay ?? DefaultPrintDelay);
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

        #region Skip

        public override void Skip(System.Action callback)
        {
            if (textTyper.IsSkippable())
            {
                textTyper.Skip();
            }
            else
            {
                PlayEmojiAnimation(StopHash);
            }
        }
        #endregion
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

    [Serializable]
    public class PrintDelay
    {
        public LanguageType languageType;
        public float delay;
    }
}
