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

            Widget.Find<HeaderMenu>().Close(true);

            stage.ClearBattle();
            stage.stageId = 0;
            stage.LoadBackground("room");
            stage.roomAnimator.Play("EnteringRoom");

            yield return new WaitForEndOfFrame();
            stage.selectedPlayer = null;
            if (!(stage.AvatarState is null))
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarState(stage.AvatarState);
            }
            var roomPosition = stage.roomPosition;

            var player = stage.GetPlayer(roomPosition - new Vector2(3.0f, 0.0f));
            player.transform.localScale = Vector3.one;
            player.SetSortingLayer(SortingLayer.NameToID("Character"), 100);
            player.StopAllCoroutines();
            player.StartRun();
            if (player.Costumes.Any(value => value.Id == 40100002))
            {
                roomPosition += new Vector2(-0.17f, -0.05f);
            }

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);
            status.Close(true);

            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();

            var stageLoadingScreen = Widget.Find<StageLoadingScreen>();
            if (stageLoadingScreen.IsActive())
            {
                stageLoadingScreen.Close();
            }
            var battle = Widget.Find<UI.Battle>();
            if (battle.IsActive())
            {
                battle.Close(true);
            }
            var battleResult = Widget.Find<BattleResult>();
            if (battleResult.IsActive())
            {
                battleResult.Close();
            }

            var loadingScreen = Widget.Find<LoadingScreen>();
            if (loadingScreen.IsActive())
            {
                loadingScreen.Close();
            }
            ActionRenderHandler.Instance.Pending = false;
            yield return new WaitForSeconds(1.0f);

            if (player)
            {
                yield return new WaitWhile(() => player.transform.position.x < roomPosition.x);
            }

            player.RunSpeed = 0.0f;
            player.Animator.Idle();

            Widget.Find<Status>().Show();
            Widget.Find<EventBanner>().Show();
            var headerMenu = Widget.Find<HeaderMenu>();
            if (!headerMenu.isActiveAndEnabled)
            {
                headerMenu.Show();
            }

            Destroy(this);
            stage.OnRoomEnterEnd.OnNext(stage);
        }
    }
}
