using Nekoyume.Game.Character;
using Nekoyume.UI.Tween;
using System.Collections;
using DG.Tweening;
using Nekoyume.Game.Factory;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GrindingLoadingScreen : ScreenWidget
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

        public RectTransform crystalAnimationStartRect;
        public RectTransform crystalAnimationTargetRect;

        private Coroutine _npcAppearCoroutine = null;
        private readonly WaitForSeconds _waitForOneSec = new WaitForSeconds(1f);

        public System.Action OnDisappear { get; set; }
        public int CrystalAnimationCount { get; set; }

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

        public void AnimateNPC(string quote)
        {
            _npcAppearCoroutine = StartCoroutine(CoAnimateNPC(quote));
        }

        public void SetItemMaterial(Item item, bool isConsumable = false)
        {
            speechBubble.SetItemMaterial(item, isConsumable);
        }

        public void SetCurrency(long ncg, long crystal)
        {
            speechBubble.SetCurrency(ncg, crystal);
        }

        public void SetCloseAction(System.Action closeAction)
        {
            _closeAction = closeAction;
        }

        private IEnumerator CoAnimateNPC(string quote = null)
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

            speechBubble.Show();
            if (quote is null)
            {
                speechBubble.SetKey("SPEECH_COMBINATION_START_");
                StartCoroutine(speechBubble.CoShowText(true));
            }
            else
            {
                StartCoroutine(speechBubble.CoShowText(quote, true));
            }

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
            if (AnimationState.Value == AnimationStateType.Shown)
            {
                TweenByClosing();
                AnimationState.Value = AnimationStateType.Closing;
                yield return new WaitWhile(() => _bgAlphaTweener.IsPlaying);
                OnCloseComplete();
            }
        }

        private void AnimateCrystalMoving()
        {
            var crystalAnimationStartPosition = crystalAnimationStartRect != null
                ? (Vector3) crystalAnimationStartRect
                    .GetWorldPositionOfCenter()
                : speechBubble.transform.position;
            var crystalAnimationTargetPosition =
                crystalAnimationTargetRect != null
                    ? (Vector3) crystalAnimationTargetRect
                        .GetWorldPositionOfCenter()
                    : Find<HeaderMenuStatic>().Crystal.IconPosition +
                      GrindModule.CrystalMovePositionOffset;
            StartCoroutine(ItemMoveAnimationFactory.CoItemMoveAnimation(
                ItemMoveAnimationFactory.AnimationItemType.Crystal,
                crystalAnimationStartPosition,
                crystalAnimationTargetPosition,
                CrystalAnimationCount));
        }

        private void OnCloseComplete()
        {
            npcSkeletonGraphic.gameObject.SetActive(false);
            speechBubble.Hide();
            OnDisappear?.Invoke();
            _closeAction?.Invoke();
            AnimateCrystalMoving();
            Close();
        }
    }
}
