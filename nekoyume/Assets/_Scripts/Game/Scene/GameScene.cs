using Cysharp.Threading.Tasks;

namespace Nekoyume.Game.Scene
{
    // TODO: LobbyScene으로 변경
    public class GameScene : BaseScene
    {
        protected override async UniTask LoadSceneAssets()
        {
            await UniTask.CompletedTask;
        }

        protected override async UniTask WaitActionResponse()
        {
            await UniTask.CompletedTask;
        }

        public override void Clear()
        {
        }
    }
}
