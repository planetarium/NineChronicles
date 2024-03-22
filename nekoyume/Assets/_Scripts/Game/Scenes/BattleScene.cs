using Cysharp.Threading.Tasks;
using Nekoyume.Game.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.UI;

namespace Nekoyume.Game.Scenes
{
    public class BattleScene : BaseScene
    {
        private BattleLog _battleLog;

        protected override UniTask LoadSceneAssets()
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask WaitActionResponse()
        {
            return UniTask.WaitUntil(HasBattleLog);
        }

        public override void Clear()
        {
            // TODO: 전투 상태 관리하는 매니저 추가해서 거기서 관리
            // 씬은 오브젝트가 언제나 존재하지 않으니까..
            _battleLog = null;

            Widget.Find<UI.Battle>().Close(true);
            Stage.instance.ClearBattle();
            Stage.instance.DestroyBackground();
        }

        private bool HasBattleLog() => _battleLog != null;

        public void OnBattleLogReceived(BattleLog log)
        {
            _battleLog = log;
            Event.OnStageStart.Invoke(log);
        }
    }
}
