using Libplanet;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ArenaRankingCellView : MonoBehaviour
    {
        public Button avatarInfoButton;
        public SubmitButton challengeButton;
        public TextMeshProUGUI rankText;
        public Image icon;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI idText;
        public TextMeshProUGUI ratingText;
        public TextMeshProUGUI cpText;
        public Image flag;
        public Tween.DOTweenRectTransformMoveBy tweenMove;
        public Tween.DOTweenGroupAlpha tweenAlpha;

        public System.Action<ArenaRankingCellView> onClickChallenge;
        public System.Action<Address> onClickInfo;
        
        public State.ArenaInfo AvatarInfo { get; private set; }

        private void Awake()
        {
            challengeButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickChallenge.Invoke(this);
            }).AddTo(gameObject);

            avatarInfoButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickInfo.Invoke(AvatarInfo.AvatarAddress);
            }).AddTo(gameObject);
        }

        public void Set(int ranking, ArenaInfo arenaInfo, bool canChallenge)
        {
            AvatarInfo = arenaInfo;
            
            rankText.text = ranking.ToString();
            icon.sprite = SpriteHelper.GetItemIcon(arenaInfo.ArmorId);
            icon.SetNativeSize();
            levelText.text = arenaInfo.Level.ToString();
            idText.text = arenaInfo.AvatarName;
            ratingText.text = arenaInfo.Score.ToString();
            cpText.text = arenaInfo.CombatPoint.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;

            challengeButton.SetSubmittable(canChallenge);
        }
    }
}
