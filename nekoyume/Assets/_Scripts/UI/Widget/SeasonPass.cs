using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class SeasonPass : Widget
    {
        [SerializeField]
        private ConditionalButton receiveBtn;

        protected override void Awake()
        {
            base.Awake();
            Game.Game.instance.SeasonPassServiceManager.SeasonPassLevel.Subscribe((level) => {
                if(Game.Game.instance.SeasonPassServiceManager.AvatarInfo == null)
                {
                    return;
                }
                receiveBtn.Interactable = level != Game.Game.instance.SeasonPassServiceManager.AvatarInfo.LastNormalClaim;
            }).AddTo(gameObject);
        }


        public void ReceiveAllBtn()
        {
            Game.Game.instance.SeasonPassServiceManager.ReceiveAll((result) =>
            {

            }, null);
        }
    }
}
