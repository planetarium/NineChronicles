using Nekoyume.State;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RankCell : RectCell<
        RankingModel,
        RankScroll.ContextModel>
    {
        [SerializeField]
        private RankCellPanel rankCell = null;

        [SerializeField]
        private RankCellPanel myInfoRankCell = null;

        public override void UpdateContent(RankingModel viewModel)
        {
            var currentAvatarAddress = States.Instance.CurrentAvatarState.address;
            var isMyInfo = viewModel.AvatarState.address.Equals(currentAvatarAddress);
            var cell = isMyInfo ? myInfoRankCell : rankCell;
            rankCell.gameObject.SetActive(!isMyInfo);
            myInfoRankCell.gameObject.SetActive(isMyInfo);

            switch (viewModel)
            {
                case AbilityRankingModel abilityRankingModel:
                    cell.SetDataAsAbility(abilityRankingModel);
                    break;
                case StageRankingModel stageRankingModel:
                    cell.SetDataAsStage(stageRankingModel);
                    break;
            }
        }
    }
}
