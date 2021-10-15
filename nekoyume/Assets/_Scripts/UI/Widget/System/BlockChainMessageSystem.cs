using System.Threading.Tasks;
using Nekoyume.BlockChain;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    /// <summary>
    /// `Agent`를 초기화하기 전에 생성된다고 전제함.
    /// </summary>
    public class BlockChainMessageSystem : SystemWidget
    {
        private enum PanelAnimationState
        {
            On,
            Off
        }

        private static readonly string[] AnimationTextAtlas = {".", "..", "..."};
        private static readonly int AnimationTextAtlasLength = AnimationTextAtlas.Length;

        public GameObject panel;
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI animationText;

        private PanelAnimationState _currentPanelAnimationState;
        private PanelAnimationState _nextPanelAnimationState;
        private int _animationTextAtlasIndex;
        private Animator _panelAnimator;

        protected override void Awake()
        {
            base.Awake();
            _panelAnimator = panel.GetComponent<Animator>();
        }

        public override void Initialize()
        {
            base.Initialize();

            panel.SetActive(false);

            _currentPanelAnimationState = PanelAnimationState.Off;
            _nextPanelAnimationState = PanelAnimationState.Off;
            _animationTextAtlasIndex = 0;

            Agent.OnEnqueueOwnGameAction += guid => _nextPanelAnimationState = PanelAnimationState.On;
            Agent.OnHasOwnTx += has => _nextPanelAnimationState = has
                ? PanelAnimationState.On
                : PanelAnimationState.Off;
        }

        protected override void Update()
        {
            if (_currentPanelAnimationState == _nextPanelAnimationState)
            {
                return;
            }

            _currentPanelAnimationState = _nextPanelAnimationState;

            if (_currentPanelAnimationState == PanelAnimationState.On)
            {
                if (!panel.activeSelf) panel.SetActive(true);
                _panelAnimator.Play("Show");
                UpdateAnimation();
            }
            else
            {
                _panelAnimator.Play("Close");
            }
        }

        private async void UpdateAnimation()
        {
            _animationTextAtlasIndex = 0;
            while (_currentPanelAnimationState == PanelAnimationState.On)
            {
                animationText.text = AnimationTextAtlas[_animationTextAtlasIndex];
                await Task.Delay(300);
                _animationTextAtlasIndex = _animationTextAtlasIndex + 1 < AnimationTextAtlasLength
                    ? _animationTextAtlasIndex + 1
                    : 0;
            }
        }
    }
}
