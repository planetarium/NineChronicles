using System;
using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.Manager;
using Nekoyume.Model.BattleStatus;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.Model.State;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";

        private const string FirstOpenCombinationKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";

        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";
        private const string FirstOpenQuestKeyFormat = "Nekoyume.UI.Menu.FirstOpenQuestKey_{0}";

        [SerializeField]
        private MainMenu btnQuest = null;

        [SerializeField]
        private MainMenu btnCombination = null;

        [SerializeField]
        private MainMenu btnShop = null;

        [SerializeField]
        private MainMenu btnRanking = null;

        [SerializeField]
        private ArenaPendingNCG arenaPendingNCG = null;

        [SerializeField]
        private SpeechBubble[] speechBubbles = null;

        [SerializeField]
        private GameObject shopExclamationMark = null;

        [SerializeField]
        private GameObject combinationExclamationMark = null;

        [SerializeField]
        private GameObject rankingExclamationMark = null;

        [SerializeField]
        private GameObject questExclamationMark = null;

        [SerializeField]
        private GuidedQuest guidedQuest = null;

        private Coroutine _coLazyClose;

        protected override void Awake()
        {
            base.Awake();

            speechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;

            guidedQuest.OnClickWorldQuestCell
                .Subscribe(_ => HackAndSlash())
                .AddTo(gameObject);
            guidedQuest.OnClickCombinationEquipmentQuestCell
                .Subscribe(_ => GoToCombinationEquipmentRecipe())
                .AddTo(gameObject);
        }

        // TODO: QuestPreparation.Quest(bool repeat) 와 로직이 흡사하기 때문에 정리할 여지가 있습니다.
        private void HackAndSlash()
        {
            var worldQuest = GuidedQuest.WorldQuest;
            if (worldQuest is null)
            {
                return;
            }

            var stageId = worldQuest.Goal;
            var sheets = Game.Game.instance.TableSheets;
            var stageRow = sheets.StageSheet.OrderedList.FirstOrDefault(row => row.Id == stageId);
            if (stageRow is null)
            {
                return;
            }

            var requiredCost = stageRow.CostAP;
            if (States.Instance.CurrentAvatarState.actionPoint < requiredCost)
            {
                // NOTE: AP가 부족합니다.
                return;
            }

            if (!sheets.WorldSheet.TryGetByStageId(stageId, out var worldRow))
            {
                return;
            }

            var worldId = worldRow.Id;

            Find<BottomMenu>().Close(true);
            Find<LoadingScreen>().Show();

            var stage = Game.Game.instance.Stage;
            stage.isExitReserved = false;
            stage.repeatStage = false;
            var player = stage.GetPlayer();
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
            ActionRenderHandler.Instance.Pending = true;
            Game.Game.instance.ActionManager
                .HackAndSlash(player, worldId, stageId)
                .Subscribe(_ =>
                {
                    LocalStateModifier.ModifyAvatarActionPoint(
                        States.Instance.CurrentAvatarState.address,
                        requiredCost);
                }, e => Find<ActionFailPopup>().Show("Action timeout during HackAndSlash."))
                .AddTo(this);
            LocalStateModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                - requiredCost);
            var props = new Value
            {
                ["StageID"] = stageId,
            };
            Mixpanel.Track("Unity/Click Guided Quest Enter Dungeon", props);
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
        }

        private void GoToCombinationEquipmentRecipe()
        {
            mixpanel.Mixpanel.Track("Unity/Click Guided Quest Combination Equipment");
            var combinationEquipmentQuest = GuidedQuest.CombinationEquipmentQuest;
            if (combinationEquipmentQuest is null)
            {
                return;
            }

            var recipeId = combinationEquipmentQuest.RecipeId;
            var subRecipeId = combinationEquipmentQuest.SubRecipeId;

            CombinationClickInternal(() =>
                Find<Combination>().ShowByEquipmentRecipe(recipeId, subRecipeId));
        }

        private void UpdateButtons()
        {
            btnQuest.Update();
            btnCombination.Update();
            btnShop.Update();
            btnRanking.Update();

            var addressHax = ReactiveAvatarState.Address.Value.ToHex();
            var firstOpenCombinationKey = string.Format(FirstOpenCombinationKeyFormat, addressHax);
            var firstOpenShopKey = string.Format(FirstOpenShopKeyFormat, addressHax);
            var firstOpenRankingKey = string.Format(FirstOpenRankingKeyFormat, addressHax);
            var firstOpenQuestKey = string.Format(FirstOpenQuestKeyFormat, addressHax);

            var combination = Find<Combination>();
            var hasNotificationOnCombination = combination.HasNotification;

            combinationExclamationMark.gameObject.SetActive(
                btnCombination.IsUnlocked &&
                (PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0 ||
                 hasNotificationOnCombination));
            shopExclamationMark.gameObject.SetActive(
                btnShop.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0);
            rankingExclamationMark.gameObject.SetActive(
                btnRanking.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenRankingKey, 0) == 0);

            var worldMap = Find<WorldMap>();
            worldMap.UpdateNotificationInfo();
            var hasNotificationInWorldmap = worldMap.hasNotification;

            questExclamationMark.gameObject.SetActive(
                (btnQuest.IsUnlocked &&
                 PlayerPrefs.GetInt(firstOpenQuestKey, 0) == 0) ||
                hasNotificationInWorldmap);
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

            if (questExclamationMark.gameObject.activeSelf)
            {
                var addressHax = ReactiveAvatarState.Address.Value.ToHex();
                var key = string.Format(FirstOpenQuestKeyFormat, addressHax);
                PlayerPrefs.SetInt(key, 1);
            }

            Mixpanel.Track("Unity/Enter Dungeon");
            _coLazyClose = StartCoroutine(CoLazyClose());
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

        public void CombinationClick(int slotIndex = -1)
        {
            CombinationClickInternal(() =>
            {
                if (slotIndex >= 0)
                {
                    Find<Combination>().Show(slotIndex);
                }
                else
                {
                    Find<Combination>().Show();
                }
            });
        }

        private void CombinationClickInternal(System.Action showAction)
        {
            if (showAction is null)
            {
                return;
            }

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
            showAction();

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

        public void UpdateGuideQuest(AvatarState avatarState)
        {
            guidedQuest.UpdateList(avatarState);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (!(_coLazyClose is null))
            {
                StopCoroutine(_coLazyClose);
                _coLazyClose = null;
            }

            guidedQuest.Hide(true);
            base.Show(ignoreShowAnimation);

            StartCoroutine(CoStartSpeeches());
            UpdateButtons();
            arenaPendingNCG.Show();
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            guidedQuest.Show(
                States.Instance.CurrentAvatarState,
                () => HelpPopup.HelpMe(100001));
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopSpeeches();

            guidedQuest.Hide(true);
            Find<BottomMenu>().Close(true);
            Find<Status>().Close(true);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoLazyClose(float duration = 1f, bool ignoreCloseAnimation = false)
        {
            StopSpeeches();

            Find<BottomMenu>().Close(true);
            Find<Status>().Close(true);
            yield return new WaitForSeconds(duration);
            base.Close(ignoreCloseAnimation);
        }

        private IEnumerator CoStartSpeeches()
        {
            yield return new WaitForSeconds(2.0f);

            while (AnimationState == AnimationStateType.Shown)
            {
                var n = speechBubbles.Length;
                while (n > 1)
                {
                    n--;
                    var k = Mathf.FloorToInt(Random.value * (n + 1));
                    var value = speechBubbles[k];
                    speechBubbles[k] = speechBubbles[n];
                    speechBubbles[n] = value;
                }

                foreach (var bubble in speechBubbles)
                {
                    yield return StartCoroutine(bubble.CoShowText());
                    yield return new WaitForSeconds(Random.Range(2.0f, 4.0f));
                }
            }
        }

        private void StopSpeeches()
        {
            StopCoroutine(CoStartSpeeches());
            foreach (var bubble in speechBubbles)
            {
                bubble.Hide();
            }
        }
    }
}
