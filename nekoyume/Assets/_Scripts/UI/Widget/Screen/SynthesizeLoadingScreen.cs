using Nekoyume.Game.Character;
using Nekoyume.UI.Tween;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Module;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SynthesizeLoadingScreen : ScreenWidget
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private CanvasGroup _buttonCanvasGroup = null;

        [SerializeField]
        private CanvasGroup _bgCanvasGroup = null;

        [SerializeField]
        private DOTweenGroupAlpha _buttonAlphaTweener = null;

        [SerializeField]
        private DOTweenGroupAlpha _bgAlphaTweener = null;

        [SerializeField]
        private TextMeshProUGUI continueText = null;

        [SerializeField]
        private SpeechBubbleWithItem speechBubble = null;

        [SerializeField]
        private SkeletonGraphic npcSkeletonGraphic;

        [SerializeField]
        private Color npcCloseColor;

        private Coroutine _npcAppearCoroutine = null;
        private readonly WaitForSeconds _waitForOneSec = new(1f);

        public System.Action OnDisappear { get; set; }

        private const int ContinueTime = 5;
        private System.Action _closeAction;

        protected override void Awake()
        {
            base.Awake();
            button.onClick.AddListener(() =>
            {
                if (_npcAppearCoroutine is not null)
                {
                    StopCoroutine(_npcAppearCoroutine);
                }

                StartCoroutine(DisappearNpc());
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _buttonCanvasGroup.alpha = 0f;
            _bgCanvasGroup.alpha = 0f;
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.text = string.Format(format, ContinueTime);
            npcSkeletonGraphic.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);
        }

        private void TweenByClosing()
        {
            _buttonAlphaTweener.PlayReverse();
            _bgAlphaTweener.PlayReverse();
            npcSkeletonGraphic.AnimationState.SetAnimation(0,
                NPCAnimation.Type.Disappear.ToString(), false);
            speechBubble.Close();
        }

        public void AnimateNpc(HashSet<(int,Grade)> resultPool)
        {
            _npcAppearCoroutine = StartCoroutine(CoAnimateNpc(resultPool));
        }

        public void SetCloseAction(System.Action closeAction)
        {
            _closeAction = closeAction;
        }

        IEnumerator CoChangeItem(HashSet<(int,Grade)> resultPool)
        {
            while (isActiveAndEnabled)
            {
                foreach (var poolItem in resultPool)
                {
                    speechBubble.SetRune(SpriteHelper.GetItemIcon(poolItem.Item1));
                    yield return new WaitForSeconds(.1f);
                }
            }
        }

        private IEnumerator CoAnimateNpc(HashSet<(int,Grade)> resultPool)
        {
            StartCoroutine(CoChangeItem(resultPool));

            yield return new WaitForSeconds(.5f);

            npcSkeletonGraphic.AnimationState.SetAnimation(0,
                NPCAnimation.Type.Appear.ToString(), false);
            npcSkeletonGraphic.AnimationState.AddAnimation(0,
                NPCAnimation.Type.Emotion_02.ToString(), false, 0f);
            npcSkeletonGraphic.AnimationState.AddAnimation(0,
                NPCAnimation.Type.Emotion_03.ToString(), true, 0f);
            npcSkeletonGraphic.color = Color.white;
            npcSkeletonGraphic.gameObject.SetActive(true);

            yield return new WaitForSeconds(1f);

            speechBubble.Show();
            speechBubble.SetKey("SPEECH_SYNTHESIZE_START");
            StartCoroutine(speechBubble.CoShowText(L10nManager.Localize("SPEECH_SYNTHESIZE_START"),true));

            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");

            for (var timer = ContinueTime; timer >= 0; --timer)
            {
                continueText.text = string.Format(format, timer);
                yield return _waitForOneSec;
            }

            StartCoroutine(DisappearNpc());
        }

        private IEnumerator CoWorkshopItemMove()
        {
            var item = speechBubble.Item;
            var target = Find<HeaderMenuStatic>()
                .GetToggle(HeaderMenuStatic.ToggleType.CombinationSlots);
            var targetPosition = target ? target.position : Vector3.zero;

            ItemMoveAnimation.Show(
                item.ItemBase.Value.GetIconSprite(),
                speechBubble.ItemView.transform.position,
                targetPosition,
                Vector2.one * 1.5f,
                false,
                false,
                1f,
                0,
                ItemMoveAnimation.EndPoint.Workshop);

            yield return null;
        }

        private IEnumerator DisappearNpc()
        {
            if (AnimationState.Value != AnimationStateType.Shown)
            {
                yield break;
            }

            if (speechBubble.Item != null)
            {
                StartCoroutine(CoWorkshopItemMove());
            }

            TweenByClosing();
            AnimationState.Value = AnimationStateType.Closing;
            yield return new WaitWhile(() => _bgAlphaTweener.IsPlaying);
            OnCloseComplete();
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
