using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AvatarInfo : Widget
    {
        [SerializeField]
        private DetailedStatView[] statViews;

        #region Override

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var player = Game.Game.instance.Stage.selectedPlayer.Model;

            var statTuples = player.Stats.GetBaseAndAdditionalStats();
            var idx = 0;
            foreach (var (statType, value, additionalValue) in statTuples)
            {
                var info = statViews[idx];
                info.Show(statType, value, additionalValue);
                ++idx;
            }
        }

        #endregion
    }
}
