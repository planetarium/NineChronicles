using UnityEngine;
using TMPro;
using System;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;
    public class SeasonPassMenu : MainMenu
    {
        [SerializeField]
        private GameObject premiumIcon;
        [SerializeField]
        private GameObject premiumPlusIcon;
        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private TextMeshProUGUI timeText;
        [SerializeField]
        private GameObject notificationObj;

        private const int SeasonPassNewPopupLimitClearedStageId = 30;

        private void Awake()
        {
            _requireStage = SeasonPassNewPopupLimitClearedStageId;
            var seasonPassService = Game.Game.instance.SeasonPassServiceManager;
            seasonPassService.AvatarInfo.Subscribe((info)=> {
                if (info == null)
                    return;

                premiumIcon.SetActive(info.IsPremium);
                premiumPlusIcon.SetActive(info.IsPremiumPlus);
                levelText.text = $"Lv.{info.Level}";
                notificationObj.SetActive(info.LastNormalClaim != info.Level);
            }).AddTo(gameObject);

            Game.Game.instance.SeasonPassServiceManager.RemainingDateTime.Subscribe((endDate) =>
            {
                timeText.text = $"<Style=Clock> {endDate}";
            });
        }
    }
}
