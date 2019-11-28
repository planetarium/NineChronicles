using System.Linq;
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RankingInfo : MonoBehaviour
    {
        public TextMeshProUGUI rank;
        public Image icon;
        public TextMeshProUGUI level;
        public TextMeshProUGUI id;
        public TextMeshProUGUI stage;
        public Image flag;
        public Tween.DOTweenRectTransformMoveBy tweenMove;
        public Tween.DOTweenGroupAlpha tweenAlpha;

        public void Set(int ranking, State.AvatarState avatarState)
        {
            rank.text = ranking.ToString();
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            var armorId = armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId;
            icon.sprite = SpriteHelper.GetItemIcon(armorId);
            icon.SetNativeSize();
            level.text = avatarState.level.ToString();
            id.text = avatarState.name;
            stage.text = avatarState.exp.ToString();
            tweenMove.StartDelay = ranking * 0.16f;
            tweenAlpha.StartDelay = ranking * 0.16f;
            //TODO 국가설정에 따라 국기가 변해야함
        }
    }
}
