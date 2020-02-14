using System.Collections;
using System.Threading.Tasks;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    /// <summary>
    /// `Agent`를 초기화하기 전에 생성된다고 전제함.
    /// </summary>
    public class BlockChainMessageBoard : SystemInfoWidget
    {
        private enum AnimationState
        {
            On,
            Off
        }

        private static readonly string[] AnimationTextAtlas = {".", "..", "..."};
        private static readonly int AnimationTextAtlasLength = AnimationTextAtlas.Length;

        public GameObject panel;
        public TextMeshProUGUI messageText;
        public TextMeshProUGUI animationText;

        private AnimationState _currentAnimationState;
        private AnimationState _nextAnimationState;
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
            messageText.text = LocalizationManager.Localize("BLOCK_CHAIN_MINING_TX");

            _currentAnimationState = AnimationState.Off;
            _nextAnimationState = AnimationState.Off;
            _animationTextAtlasIndex = 0;

            Agent.OnEnqueueOwnGameAction += guid => _nextAnimationState = AnimationState.On;
            Agent.OnHasOwnTx += has => _nextAnimationState = has
                ? AnimationState.On
                : AnimationState.Off;
        }

        protected override void Update()
        {
            if (_currentAnimationState == _nextAnimationState)
                return;

            _currentAnimationState = _nextAnimationState;

            if (_currentAnimationState == AnimationState.On)
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
            while (_currentAnimationState == AnimationState.On)
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
