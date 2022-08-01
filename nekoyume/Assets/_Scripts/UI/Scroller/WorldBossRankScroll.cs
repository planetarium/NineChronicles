using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class WorldBossRankScroll : RectScroll<WorldBossRankItem, WorldBossRankScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }

        [SerializeField]
        private WorldBossRankCell cellTemplate = null;
    }
}
