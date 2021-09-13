using System;
using Nekoyume.EnumType;
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationLoadingScreen : Widget
    {
        [SerializeField] private Button button = null;
        [SerializeField] private CanvasGroup _buttonCanvasGroup = null;
        [SerializeField] private CanvasGroup _bgCanvasGroup = null;
        [SerializeField] private DOTweenGroupAlpha _buttonAlphaTweener = null;
        [SerializeField] private DOTweenGroupAlpha _bgAlphaTweener = null;
        [SerializeField] private TextMeshProUGUI continueText = null;
        [SerializeField] private SpeechBubbleWithItem speechBubble = null;

        private NPC _npc = null;
        private Coroutine _npcAppearCoroutine = null;
        private readonly WaitForSeconds _waitForOneSec = new WaitForSeconds(1f);

        private CombinationSparkVFX _sparkVFX = null;

        public System.Action OnDisappear { get; set; }

        private const int ContinueTime = 5;
        private const int NPCId = 300001;
        private System.Action _closeAction;

        private static readonly Vector3 NPCPosition = new Vector3(1000f, 999.2f, 2.15f);

        public override WidgetType WidgetType => WidgetType.Screen;

        protected override void Awake()
        {
            base.Awake();
            button.onClick.AddListener(DisappearNPC);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _buttonCanvasGroup.alpha = 0f;
            _bgCanvasGroup.alpha = 0f;
            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");
            continueText.text = string.Format(format, ContinueTime);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (!(_npc is null))
            {
                _npc.gameObject.SetActive(false);
            }

            if (_sparkVFX)
            {
                _sparkVFX.Stop();
                _sparkVFX = null;
            }

            base.Close(ignoreCloseAnimation);
        }

        public void ShowButton()
        {
            _buttonAlphaTweener.Play();
            _bgAlphaTweener.Play();
        }

        public void HideButton()
        {
            _buttonAlphaTweener.PlayReverse();
            _bgAlphaTweener.PlayReverse();
        }

        public void AnimateNPC(Nekoyume.Model.Item.ItemType itemType)
        {
            _npcAppearCoroutine = StartCoroutine(CoAnimateNPC(itemType));
        }

        public void AnimateNPC(Nekoyume.Model.Item.ItemType itemType, string quote)
        {
            _npcAppearCoroutine = StartCoroutine(CoAnimateNPC(itemType, quote));
        }

        public void DisappearNPC()
        {
            if (!(_npcAppearCoroutine is null))
                StopCoroutine(_npcAppearCoroutine);
            StartCoroutine(CoDisappearNPC());
        }

        public void SetItemMaterial(Item item, bool isConsumable = false)
        {
            speechBubble.SetItemMaterial(item, isConsumable);
        }

        public void SetCloseAction(System.Action closeAction)
        {
            _closeAction = closeAction;
        }

        private IEnumerator CoAnimateNPC(Nekoyume.Model.Item.ItemType itemType, string quote = null)
        {
            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                NPCPosition,
                LayerType.UI,
                31);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear(.3f);
            ShowButton();
            var pos = ActionCamera.instance.Cam.transform.position;
            _sparkVFX = VFXController.instance.CreateAndChaseCam<CombinationSparkVFX>(pos);
            _npc.PlayAnimation(itemType switch
            {
                ItemType.Consumable => NPCAnimation.Type.Appear_03,
                ItemType.Equipment => NPCAnimation.Type.Appear_02,
            });
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
            StartCoroutine(CoWorkshopItemMove());

            var format = L10nManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");

            for (var timer = ContinueTime; timer >= 0; --timer)
            {
                continueText.text = string.Format(format, timer);
                yield return _waitForOneSec;
            }

            StartCoroutine(CoDisappearNPC());
        }

        private IEnumerator CoWorkshopItemMove()
        {
            yield return new WaitForSeconds(speechBubble.bubbleTweenTime);

            var item = speechBubble.item;
            var target = Find<HeaderMenu>().GetToggle(HeaderMenu.ToggleType.CombinationSlots);
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

        private IEnumerator CoDisappearNPC()
        {
            _npc.PlayAnimation(NPCAnimation.Type.Disappear_02);
            HideButton();
            if (_sparkVFX)
            {
                _sparkVFX.LazyStop();
            }

            yield return new WaitForSeconds(.5f);
            _npc.gameObject.SetActive(false);
            speechBubble.Hide();
            OnDisappear?.Invoke();
            _closeAction?.Invoke();
            Close();
        }
    }
}
