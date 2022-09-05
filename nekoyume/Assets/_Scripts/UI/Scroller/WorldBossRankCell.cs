using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class WorldBossRankCell : RectCell<WorldBossRankItem, WorldBossRankScroll.ContextModel>
    {
        [SerializeField]
        private WorldBossRankItemView view;

        public override void UpdateContent(WorldBossRankItem viewModel)
        {
            view.Set(viewModel, Context);
        }
    }
}
