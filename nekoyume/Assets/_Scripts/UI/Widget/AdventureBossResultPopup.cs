using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

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

