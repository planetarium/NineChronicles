#nullable enable

using Nekoyume.Model.BattleStatus;
using System;

namespace Nekoyume.Game.BattleRender
{
    // Stage, WorldBoss, Arena등 전투결과 렌더링과 리소스 관리하는 클래스
    public class BattleRenderManager
    {
        #region Singleton
        private static class Singleton
        {
            internal static readonly BattleRenderManager Value = new();
        }

        public static BattleRenderManager Instance => Singleton.Value;
        #endregion Singleton

        #region Fields
        private Action<BattleLog>? _onStageStart;
        #endregion Fields

        #region Properties & Events

        public bool IsOnLoading => IsWaitingBattleLog;

        // TODO: Remove
        public bool IsWaitingBattleLog { get; set; }

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
