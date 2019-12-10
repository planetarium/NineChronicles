using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
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
        
        public State.AvatarState AvatarState { get; private set; }

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClick.Invoke(this);
            }).AddTo(gameObject);
        }

        public void Set(int ranking, State.AvatarState avatarState)
        {
            AvatarState = avatarState;
            
            rank.text = ranking.ToString();
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            var armorId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            icon.sprite = SpriteHelper.GetItemIcon(armorId);
            icon.SetNativeSize();
            level.text = avatarState.level.ToString();
            id.text = avatarState.NameWithHash;
            stage.text = avatarState.exp.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
            //TODO 국가설정에 따라 국기가 변해야함
        }
    }
}
