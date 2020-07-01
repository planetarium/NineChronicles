using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.UI.Tween;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationLoadingScreen : ScreenWidget
    {
        // TODO : Combination에서 처리중인 조합 연출이 로딩화면으로 이동해야함
        // NOTE : NPC 애니메이션에 의존적인 부분이 있어 애니메이션 대신 트윈을 사용합니다.

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private CanvasGroup _canvasGroup = null;

        [SerializeField]
        private DOTweenGroupAlpha _buttonAlphaTweener = null;

        [SerializeField]
        private Transform npcPosition = null;

        private NPC _npc = null;

        private Coroutine _npcAppearCoroutine = null;

        public System.Action OnDisappear { get; set; }

        private const int NPCId = 300001;

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
                100);
            _npc = go.GetComponent<NPC>();
            _npc.SpineController.Appear(.3f);
            ShowButton();
            _npc.PlayAnimation(NPCAnimation.Type.Appear_02);
            yield return new WaitForSeconds(10f);
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
