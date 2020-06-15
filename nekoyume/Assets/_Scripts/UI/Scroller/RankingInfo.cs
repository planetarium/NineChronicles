using Libplanet;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RankingInfo : MonoBehaviour
    {
        [SerializeField]
        private Button button = null;
        [SerializeField]
        private TextMeshProUGUI rank = null;
        [SerializeField]
        private VanillaCharacterView characterView = null;
        [SerializeField]
        private TextMeshProUGUI level = null;
        [SerializeField]
        private TextMeshProUGUI id = null;
        [SerializeField]
        private TextMeshProUGUI stage = null;
        [SerializeField]
        private Image flag = null;
        [SerializeField]
        private Tween.DOTweenRectTransformMoveBy tweenMove = null;
        [SerializeField]
        private Tween.DOTweenGroupAlpha tweenAlpha = null;

        public System.Action<(RectTransform rectTransform, Address avatarAddress)> onClick;

        private RectTransform _rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (!_rectTransform)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        public Nekoyume.Model.State.RankingInfo AvatarInfo { get; private set; }

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClick?.Invoke((RectTransform, AvatarInfo.AvatarAddress));
            }).AddTo(gameObject);
        }

        public void Set(int ranking, Nekoyume.Model.State.RankingInfo avatarState)
        {
            AvatarInfo = avatarState;

            rank.text = ranking.ToString();
            characterView.SetByAvatarAddress(avatarState.AvatarAddress);
            level.text = avatarState.Level.ToString();
            id.text = avatarState.AvatarName;
            stage.text = avatarState.Exp.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
        }
    }
}
