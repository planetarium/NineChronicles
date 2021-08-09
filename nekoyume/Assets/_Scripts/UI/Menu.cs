using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.Model.BattleStatus;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;
using mixpanel;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.State.Subjects;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Menu : Widget
    {
        private const string FirstOpenShopKeyFormat = "Nekoyume.UI.Menu.FirstOpenShopKey_{0}";

        private const string FirstOpenCombinationKeyFormat =
            "Nekoyume.UI.Menu.FirstOpenCombinationKey_{0}";

        private const string FirstOpenRankingKeyFormat = "Nekoyume.UI.Menu.FirstOpenRankingKey_{0}";
        private const string FirstOpenQuestKeyFormat = "Nekoyume.UI.Menu.FirstOpenQuestKey_{0}";
        private const string FirstOpenMimisbrunnrKeyFormat = "Nekoyume.UI.Menu.FirstOpenMimisbrunnrKeyKey_{0}";

        [SerializeField]
        private MainMenu btnQuest = null;

        [SerializeField]
        private MainMenu btnCombination = null;

        [SerializeField]
        private MainMenu btnShop = null;

        [SerializeField]
        private MainMenu btnRanking = null;

        [SerializeField]
        private MainMenu btnMimisbrunnr = null;

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
        private GameObject mimisbrunnrExclamationMark = null;

        [SerializeField]
        private GuidedQuest guidedQuest = null;

        [SerializeField]
        private ActionPoint actionPoint;

        private Coroutine _coLazyClose;

        public SpriteRenderer combinationSpriteRenderer;
        public SpriteRenderer hasSpriteRenderer;

        protected override void Awake()
        {
            base.Awake();

            speechBubbles = GetComponentsInChildren<SpeechBubble>();
            Game.Event.OnRoomEnter.AddListener(b => Show());

            CloseWidget = null;

            guidedQuest.OnClickWorldQuestCell
                .Subscribe(tuple => HackAndSlash(tuple.quest.Goal))
                .AddTo(gameObject);
            guidedQuest.OnClickCombinationEquipmentQuestCell
                .Subscribe(tuple => GoToCombinationEquipmentRecipe(tuple.quest.RecipeId))
                .AddTo(gameObject);
        }

        // TODO: QuestPreparation.Quest(bool repeat) 와 로직이 흡사하기 때문에 정리할 여지가 있습니다.
        private void HackAndSlash(int stageId)
        {
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
                    LocalLayerModifier.ModifyAvatarActionPoint(
                        States.Instance.CurrentAvatarState.address,
                        requiredCost);
                }, e => ActionRenderHandler.BackToMain(false, e))
                .AddTo(this);
            LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
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

        private void GoToCombinationEquipmentRecipe(int recipeId)
        {
            Mixpanel.Track("Unity/Click Guided Quest Combination Equipment");

            CombinationClickInternal(() =>
                Find<Combination>().ShowByEquipmentRecipe(recipeId));
        }

        private void UpdateButtons()
        {
            btnQuest.Update();
            btnCombination.Update();
            btnShop.Update();
            btnRanking.Update();
            btnMimisbrunnr.Update();

            var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
            var firstOpenCombinationKey = string.Format(FirstOpenCombinationKeyFormat, addressHex);
            var firstOpenShopKey = string.Format(FirstOpenShopKeyFormat, addressHex);
            var firstOpenRankingKey = string.Format(FirstOpenRankingKeyFormat, addressHex);
            var firstOpenQuestKey = string.Format(FirstOpenQuestKeyFormat, addressHex);
            var firstOpenMimisbrunnrKey = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);

            var combination = Find<Combination>();
            var hasNotificationOnCombination = combination.HasNotification;

            combinationExclamationMark.gameObject.SetActive(
                btnCombination.IsUnlocked &&
                (PlayerPrefs.GetInt(firstOpenCombinationKey, 0) == 0 ||
                 hasNotificationOnCombination));
            shopExclamationMark.gameObject.SetActive(
                btnShop.IsUnlocked &&
                PlayerPrefs.GetInt(firstOpenShopKey, 0) == 0);

            var currentAddress = States.Instance.CurrentAvatarState?.address;
            if (currentAddress != null)
            {
                var arenaInfo = States.Instance.WeeklyArenaState.GetArenaInfo(currentAddress.Value);
                rankingExclamationMark.gameObject.SetActive(
                    btnRanking.IsUnlocked &&
                    (arenaInfo == null || arenaInfo.DailyChallengeCount > 0));
            }

            var worldMap = Find<WorldMap>();
            worldMap.UpdateNotificationInfo();
            var hasNotificationInWorldmap = worldMap.HasNotification;

            questExclamationMark.gameObject.SetActive(
                (btnQuest.IsUnlocked &&
                 PlayerPrefs.GetInt(firstOpenQuestKey, 0) == 0) ||
                hasNotificationInWorldmap);

            mimisbrunnrExclamationMark.gameObject.SetActive(
                (btnMimisbrunnr.IsUnlocked &&
                 PlayerPrefs.GetInt(firstOpenMimisbrunnrKey, 0) == 0) ||
                hasNotificationInWorldmap);
        }

        private void HideButtons()
        {
            btnQuest.gameObject.SetActive(false);
            btnCombination.gameObject.SetActive(false);
            btnShop.gameObject.SetActive(false);
            btnRanking.gameObject.SetActive(false);
            btnMimisbrunnr.gameObject.SetActive(false);
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
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenQuestKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            _coLazyClose = StartCoroutine(CoLazyClose());
            var avatarState = States.Instance.CurrentAvatarState;
            Find<WorldMap>().Show(avatarState.worldInformation);
            AudioController.PlayClick();
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
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenShopKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            Find<ShopBuy>().Show();
            AudioController.PlayClick();
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
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenCombinationKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            Close();
            showAction();

            AudioController.PlayClick();
        }

        public void RankingClick()
        {
            if (!btnRanking.IsUnlocked)
            {
                btnRanking.JingleTheCat();
                return;
            }

            Close();
            Find<RankingBoard>().Show();
            AudioController.PlayClick();
        }

        public void MimisbrunnrClick()
        {
            if (!btnMimisbrunnr.IsUnlocked)
            {
                btnMimisbrunnr.JingleTheCat();
                return;
            }

            const int worldId = GameConfig.MimisbrunnrWorldId;
            var worldSheet = Game.Game.instance.TableSheets.WorldSheet;
            var worldRow =
                worldSheet.OrderedList.FirstOrDefault(
                    row => row.Id == worldId);
            if (worldRow is null)
            {
                Notification.Push(MailType.System, L10nManager.Localize("ERROR_WORLD_DOES_NOT_EXIST"));
                return;
            }

            var wi = States.Instance.CurrentAvatarState.worldInformation;
            if (!wi.TryGetWorld(worldId, out var world))
            {
                LocalLayerModifier.AddWorld(
                    States.Instance.CurrentAvatarState.address,
                    worldId);

                if (!wi.TryGetWorld(worldId, out world))
                {
                    // Do nothing.
                    return;
                }
            }

            if (!world.IsUnlocked)
            {
                // Do nothing.
                return;
            }

            var SharedViewModel = new WorldMap.ViewModel
            {
                WorldInformation = wi,
            };

            if (mimisbrunnrExclamationMark.gameObject.activeSelf)
            {
                var addressHex = States.Instance.CurrentAvatarState.address.ToHex();
                var key = string.Format(FirstOpenMimisbrunnrKeyFormat, addressHex);
                PlayerPrefs.SetInt(key, 1);
            }

            _coLazyClose = StartCoroutine(CoLazyClose());
            AudioController.PlayClick();

            SharedViewModel.SelectedWorldId.SetValueAndForceNotify(world.Id);
            SharedViewModel.SelectedStageId.SetValueAndForceNotify(world.GetNextStageId());
            var stageInfo = Find<UI.StageInformation>();
            stageInfo.Show(SharedViewModel, worldRow, StageInformation.StageType.Mimisbrunnr);
            var status = Find<Status>();
            status.Close(true);
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
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            // TODO: move invocation the PlayTutorial() inside of CoHelpPopup().
            Find<Dialog>().Show(1, PlayTutorial);
            StartCoroutine(CoHelpPopup());
        }

        private void PlayTutorial()
        {
            var tutorialController = Game.Game.instance.Stage.TutorialController;
            var tutorialProgress = tutorialController.GetTutorialProgress();
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            var nextStageId = avatarState.worldInformation
                .TryGetLastClearedStageId(out var stageId) ? stageId + 1 : 1;

            if (nextStageId > 4)
            {
                return;
            }

            if (tutorialProgress <= 1)
            {
                if (nextStageId <= 3)
                {
                    tutorialController.Play(1);
                    return;
                }
                else
                {
                    tutorialController.SaveTutorialProgress(1);
                    tutorialProgress = 1;
                }
            }

            if (tutorialProgress == 1)
            {
                var recipeRow = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.OrderedList
                    .FirstOrDefault();
                if (recipeRow is null)
                {
                    Debug.LogError("EquipmentItemRecipeSheet is empty");
                    return;
                }

                if (!States.Instance.CurrentAvatarState.inventory.HasItem(recipeRow.MaterialId))
                {
                    tutorialController.SaveTutorialProgress(2);
                }
                else
                {
                    tutorialController.Play(2);
                }
            }
        }

        private IEnumerator CoHelpPopup()
        {
            var dialog = Find<Dialog>();
            while (dialog.IsActive())
            {
                yield return null;
            }

            guidedQuest.Show(States.Instance.CurrentAvatarState);
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

        public void TutorialActionHackAndSlash() => HackAndSlash(GuidedQuest.WorldQuest?.Goal ?? 1);

        public void TutorialActionGoToFirstRecipeCellView()
        {
            var firstRecipeRow = Game.Game.instance.TableSheets.EquipmentItemRecipeSheet.OrderedList
                .FirstOrDefault(row => row.UnlockStage == 3);
            if (firstRecipeRow is null)
            {
                Debug.LogError("TutorialActionGoToFirstRecipeCellView() firstRecipeRow is null");
                return;
            }

            // Temporarily Lock tutorial recipe.
            var combination = Find<Combination>();
            combination.LoadRecipeVFXSkipMap();
            var skipMap = combination.RecipeVFXSkipMap;
            if (skipMap.ContainsKey(firstRecipeRow.Id))
            {
                skipMap.Remove(firstRecipeRow.Id);
            }
            combination.SaveRecipeVFXSkipMap();
            GoToCombinationEquipmentRecipe(firstRecipeRow.Id);
        }

        public void TutorialActionClickGuidedQuestWorldStage2()
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            player.DisableHudContainer();
            HackAndSlash(GuidedQuest.WorldQuest?.Goal ?? 4);
        }
    }
}
