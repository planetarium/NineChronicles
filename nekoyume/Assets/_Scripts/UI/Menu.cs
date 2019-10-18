using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using Nekoyume.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        public GameObject btnQuest;
        public Text btnQuestText;
        public GameObject btnCombination;
        public Text btnCombinationText;
        public GameObject btnShop;
        public Text btnShopText;
        public GameObject btnRanking;
        public Text btnRankingText;
        public Text LabelInfo;
        public SpeechBubble[] SpeechBubbles;

        public Stage Stage;

        protected override void Awake()
        {
            base.Awake();

            btnQuestText.text = LocalizationManager.Localize("UI_DUNGEON");
            btnCombinationText.text = LocalizationManager.Localize("UI_COMBINATION");
            btnShopText.text = LocalizationManager.Localize("UI_SHOP");
            btnRankingText.text = LocalizationManager.Localize("UI_RANKING");

            Stage = GameObject.Find("Stage").GetComponent<Stage>();
            SpeechBubbles = GetComponentsInChildren<SpeechBubble>();
        }

        public void ShowButtons(bool value)
        {
            btnQuest.SetActive(value);
            btnCombination.SetActive(value);
            btnShop.SetActive(value);
            btnRanking.SetActive(value);
        }

        public void ShowRoom()
        {
            var stage = Game.Game.instance.stage;
            stage.LoadBackground("room");
            stage.GetPlayer(stage.roomPosition);

            var player = stage.GetPlayer();
            player.UpdateSet(player.Model.Value.armor);
            player.gameObject.SetActive(true);

            Show();
            ShowButtons(true);
            StartCoroutine(ShowSpeeches());

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
            Close();
            var avatarState = States.Instance.CurrentAvatarState.Value;
            Find<WorldMap>().Show(avatarState.worldStage);
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainBattle);
        }

        public void ShopClick()
        {
            Close();
            Find<Shop>().Show();
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainShop);
        }

        public void CombinationClick()
        {
            Close();
            Find<Combination>().Show();
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainCombination);
        }

        public void RankingClick()
        {
            Close();
            Find<RankingBoard>().Show();
            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();
            Find<Status>().Show();
            Find<BottomMenu>().Show(UINavigator.NavigationType.Quit, _ => Game.Game.Quit());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopCoroutine(ShowSpeeches());
            foreach (var speechBubble in SpeechBubbles)
            {
                speechBubble.Hide();
            }
            
            Find<Inventory>().Close(ignoreCloseAnimation);
            Find<StatusDetail>().Close(ignoreCloseAnimation);
            Find<Quest>().Close(ignoreCloseAnimation);
            
            Find<BottomMenu>().Close(ignoreCloseAnimation);
            Find<Status>().Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator ShowSpeeches()
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
                    yield return StartCoroutine(speechBubble.CoShowText());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }
    }
}
