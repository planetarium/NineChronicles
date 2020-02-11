using System.Collections;
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
            Widget.Find<LoadingScreen>().Show();
            Widget.Find<BottomMenu>().Close();
            Widget.Find<UI.Inventory>().Close();
            Widget.Find<StatusDetail>().Close();
            Widget.Find<Quest>().Close();

            var stage = Game.instance.Stage;
            stage.stageId = 0;
            stage.LoadBackground("room");

            yield return new WaitForEndOfFrame();
            stage.selectedPlayer = null;
            if (!(stage.AvatarState is null))
            {
                ActionRenderHandler.Instance.UpdateCurrentAvatarState(stage.AvatarState);
            }

            var player = stage.GetPlayer(stage.roomPosition - new Vector2(3.0f, 0.0f));
            player.StartRun();

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);
            status.Close(true);

            ActionCamera.instance.SetPoint(0f, 0f);
            ActionCamera.instance.Idle();

            yield return new WaitForSeconds(1.0f);
            Widget.Find<LoadingScreen>().Close();

            if (player)
                while (player.transform.position.x < stage.roomPosition.x)
                {
                    yield return null;
                }

            player.RunSpeed = 0.0f;
            player.Animator.Idle();

            Widget.Find<Dialog>().Show(1);
            Widget.Find<Status>().Show();
            Widget.Find<BottomMenu>().Show(
                UINavigator.NavigationType.Quit,
                _ => Game.Quit(),
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.Character,
                BottomMenu.ToggleableType.Inventory,
                BottomMenu.ToggleableType.Settings);

            Destroy(this);
        }
    }
}
