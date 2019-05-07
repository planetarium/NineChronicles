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

        public void Set(int ranking, Nekoyume.Model.Avatar avatar)
        {
            rank.text = ranking.ToString();
            var set = avatar.Items.Select(i => i.Item).OfType<SetItem>().FirstOrDefault(e => e.equipped);
            icon.overrideSprite = ItemBase.GetSprite(set);
            icon.SetNativeSize();
            level.text = avatar.Level.ToString();
            id.text = avatar.Name;
            stage.text = avatar.WorldStage.ToString();
            //TODO 국가설정에 따라 국기가 변해야함
        }
    }
}
