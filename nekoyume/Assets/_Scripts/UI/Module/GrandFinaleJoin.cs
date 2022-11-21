using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module.Arena.Join;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GrandFinaleJoin : MonoBehaviour
    {
        private const string OffSeasonString = "off-season";

        [SerializeField]
        private ArenaJoinSeasonInfo arenaJoinSeasonInfo;

        [SerializeField]
        private ConditionalButton arenaJoinButton;

        [SerializeField]
        private ConditionalButton grandFinaleJoinButton;

        public void Set(System.Action onClickJoinArena)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var roundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            arenaJoinSeasonInfo.SetData(
                OffSeasonString,
                roundData,
                ArenaJoinSeasonInfo.RewardType.Food,
                null
            );
            arenaJoinButton.OnClickSubject.Subscribe(_ => onClickJoinArena.Invoke()).AddTo(gameObject);

            var grandFinaleRow =
                TableSheets.Instance.GrandFinaleScheduleSheet.GetRowByBlockIndex(blockIndex);
            grandFinaleJoinButton.OnClickSubject.Subscribe(_ =>
            {
                AudioController.PlayClick();
                Widget.Find<ArenaJoin>().Close();
                Widget.Find<ArenaBoard>()
                    .Show(grandFinaleRow, States.Instance.GrandFinaleStates.GrandFinaleParticipants);
            }).AddTo(gameObject);
        }
    }
}
