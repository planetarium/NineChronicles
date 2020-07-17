using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.UI.Tween;
using System.Collections;
using _Scripts.UI;
using Nekoyume.UI.Model;
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
        private CanvasGroup _buttonCanvasGroup = null;

        [SerializeField]
        private CanvasGroup _bgCanvasGroup = null;

        [SerializeField]
        private DOTweenGroupAlpha _buttonAlphaTweener = null;

        [SerializeField]
        private DOTweenGroupAlpha _bgAlphaTweener = null;

        [SerializeField]
        private Transform npcPosition = null;

        [SerializeField]
        private TextMeshProUGUI continueText = null;

        [SerializeField]
        private SpeechBubbleWithItem speechBubble = null;

        private NPC _npc = null;
        private Coroutine _npcAppearCoroutine = null;
        private WaitForSeconds _waitForOneSec = new WaitForSeconds(1f);

        private CombinationSparkVFX _sparkVFX = null;
        private CombinationBGFireVFX _fireVFX = null;

        public System.Action OnDisappear { get; set; }

        private const int ContinueTime = 3;
        private const int NPCId = 300001;

        protected override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();
            button.onClick.AddListener(DisappearNPC);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _buttonCanvasGroup.alpha = 0f;
            _bgCanvasGroup.alpha = 0f;
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if(!(_npc is null))
            {
                _npc.gameObject.SetActive(false);
            }

            if (_sparkVFX)
            {
                _sparkVFX.Stop();
                _sparkVFX = null;
            }

            if (_fireVFX)
            {
                _fireVFX.Stop();
                _fireVFX = null;
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

        public void SetItemMaterial(Item item)
        {
            speechBubble.SetItemMaterial(item);
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
            var pos = ActionCamera.instance.Cam.transform.position;
            _sparkVFX = VFXController.instance.CreateAndChaseCam<CombinationSparkVFX>(pos);
            _npc.PlayAnimation(NPCAnimation.Type.Appear_02);
            yield return new WaitForSeconds(1f);
            _fireVFX = VFXController.instance.CreateAndChaseCam<CombinationBGFireVFX>(pos, new Vector3(-.7f, -.35f));
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
            if (_sparkVFX)
            {
                _sparkVFX.LazyStop();
            }
            if (_fireVFX)
            {
                _fireVFX.LazyStop();
            }
            yield return new WaitForSeconds(.5f);
            _npc.gameObject.SetActive(false);
            OnDisappear?.Invoke();
            Close();
        }
    }
}
