using Nekoyume.Game;
using Nekoyume.UI.Module.Arena.Join;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class GrandFinaleJoin : MonoBehaviour
    {
        private const string OffSeasonString = "off-season";
        public ArenaJoinSeasonInfo arenaJoinSeasonInfo;

        [SerializeField]
        private ConditionalButton arenaJoinButton;

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
        }
    }
}
