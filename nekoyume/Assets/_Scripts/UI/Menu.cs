using System.Collections;
using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        public GameObject btnQuest;
        public GameObject btnCombine;
        public GameObject btnShop;
        public GameObject btnTemple;
        public Text LabelInfo;
        
        public void ShowButtons(bool value)
        {
            btnQuest.SetActive(value);
            btnCombine.SetActive(value);
            btnShop.SetActive(value);
            btnTemple.SetActive(value);
        }

        public void ShowRoom()
        {
            Show();
            ShowButtons(true);

            LabelInfo.text = "";
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }

        public void ShowWorld()
        {
            Show();
            ShowButtons(false);

            LabelInfo.text = "";
        }

        public void QuestClick()
        {
            Find<Status>()?.Close();
            Close();
            
            Find<QuestPreparation>()?.Show();
            Find<Gold>()?.Show();
            AudioController.PlayClick();
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickMainBattle);
        }

        public void ShopClick()
        {
            Find<Status>()?.Close();
            Close();
            
            Find<Shop>().Show();
            Find<Gold>()?.Show();
            AudioController.PlayClick();
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickMainShop);
        }

        public void CombinationClick()
        {
            Find<Status>()?.Close();
            Close();
            
            Find<Combination>()?.Show();
            Find<Gold>()?.Show();
            AudioController.PlayClick();
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickMainCombination);
        }

        public override void Show()
        {
            base.Show();
            
            Find<Gold>()?.Show();
        }

        public override void Close()
        {
            Find<Gold>()?.Close();
            
            base.Close();
        }
    }
}
