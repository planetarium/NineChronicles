using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class RoomEntering : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Act());
        }

        private IEnumerator Act()
        {
            var stage = Game.instance.Stage;
            if (stage.showLoadingScreen)
            {
                Widget.Find<LoadingScreen>().Show();
            }

            Widget.Find<HeaderMenuStatic>().Close(true);

            stage.ClearBattle();
            stage.stageId = 0;

            yield return new WaitForEndOfFrame();
            stage.SelectedPlayer = null;
            if (!(stage.AvatarState is null))
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarStateAsync(stage.AvatarState);
            }
            var startPosition = new Vector2(5000.2f, 4999.1f);
            var player = stage.GetPlayer(startPosition - new Vector2(3.0f, 0.0f));
            player.transform.localScale = Vector3.one;
            player.SetSortingLayer(SortingLayer.NameToID("Character"), 100);
            player.StopAllCoroutines();
            player.StartRun();

            Widget.Find<Menu>().UpdatePlayerReactButton(player.Animator.Touch);

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);
            status.Close(true);

            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();

            var stageLoadingScreen = Widget.Find<StageLoadingEffect>();
            if (stageLoadingScreen.IsActive())
            {
                stageLoadingScreen.Close();
            }

            var battle = Widget.Find<UI.Battle>();
            if (battle.IsActive())
            {
                battle.Close(true);
            }

            var battleResult = Widget.Find<BattleResultPopup>();
            if (battleResult.IsActive())
            {
                battleResult.Close();
            }

            var loadingScreen = Widget.Find<LoadingScreen>();
            if (loadingScreen.IsActive())
            {
                loadingScreen.Close();
            }

            var arenaBattleLoadingScreen = Widget.Find<ArenaBattleLoadingScreen>();
            if (arenaBattleLoadingScreen.IsActive())
            {
                arenaBattleLoadingScreen.Close();
            }

            ActionRenderHandler.Instance.Pending = false;
            yield return new WaitForSeconds(1.0f);

            if (player)
            {
                yield return new WaitWhile(() => player.transform.position.x < startPosition.x);
            }

            player.RunSpeed = 0.0f;
            player.Animator.Idle();

            Widget.Find<Status>().Show();
            Widget.Find<EventBanner>().Show();
            Widget.Find<NoticePopup>().Show();
            var headerMenu = Widget.Find<HeaderMenuStatic>();
            if (!headerMenu.isActiveAndEnabled)
            {
                headerMenu.Show();
            }

            Destroy(this);
            stage.OnRoomEnterEnd.OnNext(stage);
        }
    }
}
