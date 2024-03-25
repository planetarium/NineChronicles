using Nekoyume.Game.Character;
using Nekoyume.UI.Tween;
using System.Collections;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RuneCombineResultScreen : ScreenWidget
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private CanvasGroup _buttonCanvasGroup;

        [SerializeField]
        private CanvasGroup _bgCanvasGroup;

        [SerializeField]
        private DOTweenGroupAlpha _buttonAlphaTweener;

        [SerializeField]
        private DOTweenGroupAlpha _bgAlphaTweener;

        [SerializeField]
        private TextMeshProUGUI continueText;

        [SerializeField]
        private SpeechBubbleWithItem speechBubble;

        [SerializeField]
        private SkeletonGraphic npcSkeletonGraphic;

        private Coroutine _npcAppearCoroutine;
        private readonly WaitForSeconds _waitForOneSec = new(1f);

        public System.Action OnDisappear { get; set; }

        private const int ContinueTime = 5;
        private System.Action _closeAction;

        protected override void Awake()
        {
            base.Awake();
            button.onClick.AddListener(() =>
            {
                if (!(_npcAppearCoroutine is null))
                {
                    StopCoroutine(_npcAppearCoroutine);
                }

                StartCoroutine(DisappearNpc());
            });
        }

        public void Show(Sprite runeIcon, string quote, bool ignoreShowAnimation = false)
        {
            _buttonCanvasGroup.alpha = 0f;
            _bgCanvasGroup.alpha = 0f;
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.text = string.Format(format, ContinueTime);
            npcSkeletonGraphic.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);

            _npcAppearCoroutine = StartCoroutine(CoAnimateNpc(runeIcon, quote));
        }

        private void TweenByClosing()
        {
            _buttonAlphaTweener.PlayReverse();
            _bgAlphaTweener.PlayReverse();
            npcSkeletonGraphic.AnimationState.SetAnimation(0,
                NPCAnimation.Type.Disappear.ToString(), false);
            speechBubble.Close();
        }

        private IEnumerator CoAnimateNpc(Sprite runeIcon, string quote)
        {
            npcSkeletonGraphic.AnimationState.SetAnimation(0,
                NPCAnimation.Type.Appear.ToString(), false);
            npcSkeletonGraphic.AnimationState.AddAnimation(0,
                NPCAnimation.Type.Emotion_02.ToString(), false, 0f);
            npcSkeletonGraphic.AnimationState.AddAnimation(0,
                NPCAnimation.Type.Emotion_03.ToString(), true, 0f);
            npcSkeletonGraphic.color = Color.white;
            npcSkeletonGraphic.gameObject.SetActive(true);

            yield return new WaitForSeconds(1f);

            speechBubble.SetRune(runeIcon);
            speechBubble.Show();
            StartCoroutine(speechBubble.CoShowText(quote, true));
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            for (var timer = ContinueTime; timer >= 0; --timer)
            {
                continueText.text = string.Format(format, timer);
                yield return _waitForOneSec;
            }

            StartCoroutine(DisappearNpc());
        }

        private IEnumerator DisappearNpc()
        {
            if (AnimationState.Value == AnimationStateType.Shown)
            {
                TweenByClosing();
                AnimationState.Value = AnimationStateType.Closing;
                yield return new WaitWhile(() => _bgAlphaTweener.IsPlaying);
                OnCloseComplete();
            }
        }

        private void OnCloseComplete()
        {
            npcSkeletonGraphic.gameObject.SetActive(false);
            speechBubble.Hide();
            OnDisappear?.Invoke();
            _closeAction?.Invoke();
            Close();
        }
    }
}
