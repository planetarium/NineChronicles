using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RankingInfo : MonoBehaviour
    {
        public Button button;
        public TextMeshProUGUI rank;
        public Image icon;
        public TextMeshProUGUI level;
        public TextMeshProUGUI id;
        public TextMeshProUGUI stage;
        public Image flag;
        public Tween.DOTweenRectTransformMoveBy tweenMove;
        public Tween.DOTweenGroupAlpha tweenAlpha;

        public System.Action<RankingInfo> onClick;
        
        public Nekoyume.Model.State.RankingInfo AvatarInfo { get; private set; }

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClick.Invoke(this);
            }).AddTo(gameObject);
        }

        public void Set(int ranking, Nekoyume.Model.State.RankingInfo avatarState)
        {
            AvatarInfo = avatarState;
            
            rank.text = ranking.ToString();
            icon.sprite = SpriteHelper.GetItemIcon(avatarState.ArmorId);
            icon.SetNativeSize();
            level.text = avatarState.Level.ToString();
            id.text = avatarState.AvatarName;
            stage.text = avatarState.Exp.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
        }
    }
}
