#nullable enable

using Nekoyume.Model.BattleStatus;
using System;

namespace Nekoyume.Game.Battle
{
    public class BattleRenderer
    {
        #region Singleton
        private static class Singleton
        {
            internal static readonly BattleRenderer Value = new();
        }

        public static BattleRenderer Instance => Singleton.Value;
        #endregion Singleton

        #region Fields
        private Action<BattleLog>? _onStageStart;
        #endregion Fields

        #region Properties & Events

        public bool IsOnLoading => IsWaitingBattleLog;

        // TODO: Remove
        public bool IsWaitingBattleLog { get; set; }

        // TODO: don't use public setter
        // TODO: UI코드에서 불리는 부분이 많은데, 다른 방식으로 체크 불가능?
        // BattlePreparation쪽에서도 set되고, battle쪽에서도 set되는데 중복처리 아닌지
        // 캐릭터 머리 위 UI도 체크해서 고치자
        public bool IsOnBattle { get; set; }

        public event Action<BattleLog>? OnStageStart
        {
            add
            {
                _onStageStart -= value;
                _onStageStart += value;
            }
            remove => _onStageStart -= value;
        }
        #endregion Properties & Events

        // 나무이동, 에셋로드, 기타등등 대기..

        // Stage, StageLoadingEffect등등.. 확인

        public void StartStage(BattleLog battleLog)
        {
            _onStageStart?.Invoke(battleLog);
        }
    }
}
