using Nekoyume.Game;
using Nekoyume.Manager;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        public GameObject btnQuest;
        public GameObject btnCombine;
        public GameObject btnShop;
        public GameObject btnTemple;
        public Text LabelInfo;
        public SpeechBubble[] SpeechBubbles;

        public Stage Stage;

        protected override void Awake()
        {
            base.Awake();

            Stage = GameObject.Find("Stage").GetComponent<Stage>();
            SpeechBubbles = GetComponentsInChildren<SpeechBubble>();
        }

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
            
            Find<Shop>()?.Show();
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

        public void RankingClick()
        {
            Find<Status>()?.Close();
            Close();

            Find<RankingBoard>()?.Show();
            Find<Gold>()?.Show();
            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();
            StartCoroutine("ShowSpeeches");
            Find<Gold>()?.Show();
        }

        public override void Close()
        {
            Find<Gold>()?.Close();
            StopCoroutine("ShowSpeeches");
            base.Close();
        }

        public IEnumerator ShowSpeeches()
        {
            foreach (var speechBubble in SpeechBubbles)
            {
                speechBubble.Init();
            }

            yield return new WaitForSeconds(2.0f);

            while (true)
            {
                var n = SpeechBubbles.Length;
                while (n > 1)
                {
                    n--;
                    var k = Mathf.FloorToInt(Random.value * (n + 1));
                    var value = SpeechBubbles[k];
                    SpeechBubbles[k] = SpeechBubbles[n];
                    SpeechBubbles[n] = value;
                }

                foreach (var speechBubble in SpeechBubbles)
                {
                    yield return StartCoroutine(speechBubble.Show());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }
    }
}
