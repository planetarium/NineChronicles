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

        public void Set(int ranking, State.AvatarState avatarState)
        {
            rank.text = ranking.ToString();
            var set = avatarState.items.Select(i => i.Item).OfType<SetItem>().FirstOrDefault(e => e.equipped);
            icon.overrideSprite = ItemBase.GetSprite(set);
            icon.SetNativeSize();
            level.text = avatarState.level.ToString();
            id.text = avatarState.name;
            stage.text = avatarState.worldStage.ToString();
            //TODO 국가설정에 따라 국기가 변해야함
        }
    }
}
