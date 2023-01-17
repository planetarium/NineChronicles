using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.UI.Tween;
using System.Collections;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationLoadingScreen : ScreenWidget
    {
        [SerializeField] private Button button = null;

        [SerializeField] private CanvasGroup _buttonCanvasGroup = null;

        [SerializeField] private CanvasGroup _bgCanvasGroup = null;

        [SerializeField] private DOTweenGroupAlpha _buttonAlphaTweener = null;

        [SerializeField] private DOTweenGroupAlpha _bgAlphaTweener = null;

        [SerializeField] private TextMeshProUGUI continueText = null;

        [SerializeField] private SpeechBubbleWithItem speechBubble = null;

        [SerializeField] private SkeletonGraphic npcSkeletonGraphic;

        private Coroutine _npcAppearCoroutine = null;
        private readonly WaitForSeconds _waitForOneSec = new WaitForSeconds(1f);

        private CombinationSparkVFX _sparkVFX = null;

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

                DisappearNpc();

                button.interactable = false;
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            button.interactable = true;
            _buttonCanvasGroup.alpha = 0f;
            _bgCanvasGroup.alpha = 0f;
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.text = string.Format(format, ContinueTime);
            npcSkeletonGraphic.gameObject.SetActive(false);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (_sparkVFX)
            {
                _sparkVFX.Stop();
                _sparkVFX = null;
            }

            base.Close(ignoreCloseAnimation);
        }

        private void HideButton()
        {
            _buttonAlphaTweener.PlayReverse();
            _bgAlphaTweener.PlayReverse();
        }

        public void AnimateNPC(Nekoyume.Model.Item.ItemType itemType, string quote)
        {
            _npcAppearCoroutine = StartCoroutine(CoAnimateNPC(itemType, quote));
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

        private IEnumerator CoAnimateNPC(Nekoyume.Model.Item.ItemType itemType, string quote = null)
        {
            var pos = ActionCamera.instance.Cam.transform.position;
            _sparkVFX = VFXController.instance.CreateAndChaseCam<CombinationSparkVFX>(pos);
            npcSkeletonGraphic.gameObject.SetActive(true);
            switch (itemType)
            {
                case ItemType.Equipment:
                    npcSkeletonGraphic.AnimationState.SetAnimation(0,
                        NPCAnimation.Type.Appear_02.ToString(), false);
                    npcSkeletonGraphic.AnimationState.AddAnimation(0,
                        NPCAnimation.Type.Loop.ToString(), true, 0f);
                    break;

                case ItemType.Consumable:
                    npcSkeletonGraphic.AnimationState.SetAnimation(0,
                        NPCAnimation.Type.Appear_03.ToString(), false);
                    npcSkeletonGraphic.AnimationState.AddAnimation(0,
                        NPCAnimation.Type.Loop_02.ToString(), true, 0f);
                    break;
            }

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

            DisappearNpc();
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

        private void DisappearNpc()
        {
            npcSkeletonGraphic.AnimationState.SetAnimation(0,
                NPCAnimation.Type.Disappear_02.ToString(), false);
            npcSkeletonGraphic.AnimationState.Complete += OnComplete;
            HideButton();
            if (speechBubble.Item != null)
            {
                StartCoroutine(CoWorkshopItemMove());
            }

            if (_sparkVFX)
            {
                _sparkVFX.LazyStop();
            }
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            npcSkeletonGraphic.gameObject.SetActive(false);
            speechBubble.Hide();
            OnDisappear?.Invoke();
            _closeAction?.Invoke();
            Close();
            npcSkeletonGraphic.AnimationState.Complete -= OnComplete;
        }
    }
}
