using System.Linq;
using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RankingInfo : MonoBehaviour
    {
        public Text rank;
        public Image icon;
        public Text level;
        public Text id;
        public Text stage;
        public Image flag;
        public Tween.DOTweenRectTransformMoveBy tweenMove;
        public Tween.DOTweenGroupAlpha tweenAlpha;

        public void Set(int ranking, State.AvatarState avatarState)
        {
            rank.text = ranking.ToString();
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            icon.sprite = Armor.GetIcon(armor);
            icon.SetNativeSize();
            level.text = avatarState.level.ToString();
            id.text = avatarState.name;
            stage.text = avatarState.worldStage.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
            //TODO 국가설정에 따라 국기가 변해야함
        }
    }
}
