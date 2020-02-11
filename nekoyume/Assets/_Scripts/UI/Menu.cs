using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.Manager;
using Nekoyume.Model;
using Nekoyume.UI.AnimatedGraphics;
using UniRx;
using UnityEngine;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";
        private const string FirstOpenCombinationKeyFormat = "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";
        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";

        public MainMenu btnQuest;
        public MainMenu btnCombination;
        public MainMenu btnShop;
        public MainMenu btnRanking;
        public ArenaPendingNCG arenaPendingNCG;
        public SpeechBubble[] SpeechBubbles;
        public NPC npc;
        public SpeechBubble speechBubble;
        public GameObject shopExclamationMark;
        public GameObject combinationExclamationMark;
        public GameObject rankingExclamationMark;

        private Coroutine _coroutine;
        private Player _player;

        protected override void Awake()
        {
            base.Awake();

            SpeechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(Show);

            CloseWidget = null;
        }

        private void ShowButtons(Player player)
        {
            btnQuest.Set(player);
            btnCombination.Set(player);
            btnShop.Set(player);
            btnRanking.Set(player);

            var addressHax = ReactiveAvatarState.Address.Value.ToHex();
            var firstOpenCombinationKey = string.Format(FirstOpenCombinationKeyFormat, addressHax);
            var firstOpenShopKey = string.Format(FirstOpenShopKeyFormat, addressHax);
            var firstOpenRankingKey = string.Format(FirstOpenRankingKeyFormat, addressHax);
            combinationExclamationMark.gameObject.SetActive(
                btnCombination.IsUnlocked && PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0);
            shopExclamationMark.gameObject.SetActive(btnShop.IsUnlocked &&
                                                     PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0);
            rankingExclamationMark.gameObject.SetActive(btnRanking.IsUnlocked &&
                                                        PlayerPrefs.GetInt(firstOpenRankingKey, 0) == 0);
        }

        private void HideButtons()
        {
            btnQuest.gameObject.SetActive(false);
            btnCombination.gameObject.SetActive(false);
            btnShop.gameObject.SetActive(false);
            btnRanking.gameObject.SetActive(false);
        }

        public void ShowWorld()
        {
            Show();
            HideButtons();
        }

        public void QuestClick()
        {
            if (!btnQuest.IsUnlocked)
            {
                btnQuest.JingleTheCat();
                return;    
            }
            
            Close();
            var avatarState = States.Instance.CurrentAvatarState;
            Find<WorldMap>().Show(avatarState.worldInformation);
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainBattle);
        }

        public void ShopClick()
        {
            if (!btnShop.IsUnlocked)
            {
                btnShop.JingleTheCat();
                return;
            }

            if (shopExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenShopKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<Shop>().Show();
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainShop);
        }

        public void CombinationClick()
        {
            if (!btnCombination.IsUnlocked)
            {
                btnCombination.JingleTheCat();
                return;
            }
            
            if (combinationExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenCombinationKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<Combination>().Show();
            AudioController.PlayClick();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickMainCombination);
        }

        public void RankingClick()
        {
            if (!btnRanking.IsUnlocked)
            {
                btnRanking.JingleTheCat();
                return;
            }

            if (rankingExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenRankingKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<RankingBoard>().Show();
            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();

            StartCoroutine(ShowSpeeches());
            ShowButtons(Game.Game.instance.Stage.selectedPlayer);
            arenaPendingNCG.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopCoroutine(ShowSpeeches());
            foreach (var bubble in SpeechBubbles)
            {
                bubble.Hide();
            }

            Find<Inventory>().Close(ignoreCloseAnimation);
            Find<StatusDetail>().Close(ignoreCloseAnimation);
            Find<Quest>().Close(ignoreCloseAnimation);

            Find<BottomMenu>().Close(true);
            Find<Status>().Close(true);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator ShowSpeeches()
        {
            ShowButtons(Game.Game.instance.Stage.selectedPlayer);

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

                foreach (var bubble in SpeechBubbles)
                {
                    yield return StartCoroutine(bubble.CoShowText());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }

        private IEnumerator CoShowRequiredLevelSpeech(string pointerClickKey, int level)
        {
            _coroutine = null;
            speechBubble.SetKey(pointerClickKey);
            var format =
                LocalizationManager.Localize(
                    $"{pointerClickKey}{Random.Range(0, speechBubble.SpeechCount)}");
            var speech = string.Format(format, level);
            yield return StartCoroutine(speechBubble.CoShowText(speech, true));
            if (npc)
            {
                npc.PlayAnimation(NPCAnimation.Type.Emotion_01);
            }

            speechBubble.ResetKey();
            if (_coroutine is null)
            {
                _coroutine = StartCoroutine(ShowSpeeches());
            }
        }
    }
}
