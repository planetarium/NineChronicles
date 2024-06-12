using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI
{
    public class AdventureBossResultPopup : Widget
    {
        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }

        public void OnClickTower()
        {
            Close();
            var worldMapLoading = Find<LoadingScreen>();
            worldMapLoading.Show();

            Find<Battle>().Close(true);
            Game.Game.instance.Stage.ReleaseBattleAssets();
            Game.Event.OnRoomEnter.Invoke(true);

            Game.Game.instance.Stage.OnRoomEnterEnd.First().Subscribe(_ =>
            {
                CloseWithOtherWidgets();
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Battle);
                var worldMap = Find<WorldMap>();
                worldMap.Show(States.Instance.CurrentAvatarState.worldInformation, true);
                Find<AdventureBoss>().Show();
                worldMapLoading.Close(true);
            });
        }
    }
}

