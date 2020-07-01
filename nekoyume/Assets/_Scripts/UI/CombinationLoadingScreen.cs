using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.UI.Tween;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationLoadingScreen : Widget
    {
        // NOTE : NPC 애니메이션에 의존적인 부분이 있어 애니메이션 대신 트윈을 사용합니다.

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private CanvasGroup _canvasGroup = null;

        [SerializeField]
        private DOTweenGroupAlpha _buttonAlphaTweener = null;

        [SerializeField]
        private Transform npcPosition = null;

        [SerializeField]
        private TextMeshProUGUI continueText = null;
        
        [SerializeField]
        private SpeechBubble speechBubble = null;

        private NPC _npc = null;
        private Coroutine _npcAppearCoroutine = null;
        private WaitForSeconds _waitForOneSec = new WaitForSeconds(1f);

        public System.Action OnDisappear { get; set; }

        private const int ContinueTime = 10;
        private const int NPCId = 300001;

        protected override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();
            button.onClick.AddListener(DisappearNPC);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _canvasGroup.alpha = 0f;
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if(!(_npc is null))
            {
                _npc.gameObject.SetActive(false);
            }

            base.Close(ignoreCloseAnimation);
        }

        public void ShowButton()
        {
            _buttonAlphaTweener.Play();
        }

        public void HideButton()
        {
            _buttonAlphaTweener.PlayReverse();
        }

        public void AnimateNPC()
        {
            _npcAppearCoroutine = StartCoroutine(CoAnimateNPC());
        }

        public void DisappearNPC()
        {
            if (!(_npcAppearCoroutine is null))
                StopCoroutine(_npcAppearCoroutine);
            StartCoroutine(CoDisappearNPC());
        }

        private IEnumerator CoAnimateNPC()
        {
            var go = Game.Game.instance.Stage.npcFactory.Create(
                NPCId,
                npcPosition.position,
                LayerType.UI,
                31);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear(.3f);
            ShowButton();
            _npc.PlayAnimation(NPCAnimation.Type.Appear_02);
            yield return new WaitForSeconds(1f);
            speechBubble.SetKey("SPEECH_COMBINATION_START_");
            StartCoroutine(speechBubble.CoShowText(true));

            var format = LocalizationManager.Localize("UI_PRESS_TO_CONTINUE_FORMAT");

            for (int timer = ContinueTime; timer >= 0; --timer)
            {
                continueText.text = string.Format(format, timer);
                yield return _waitForOneSec;
            }

            StartCoroutine(CoDisappearNPC());
        }

        private IEnumerator CoDisappearNPC()
        {
            _npc.PlayAnimation(NPCAnimation.Type.Disappear_02);
            HideButton();
            yield return new WaitForSeconds(.5f);
            _npc.gameObject.SetActive(false);
            OnDisappear?.Invoke();
            Close();
        }
    }
}
