using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Manager;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BattleResult : PopupWidget
    {
        public class Model
        {
            private readonly List<CountableItem> _rewards = new List<CountableItem>();

            public BattleLog.Result State;
            public string WorldName;
            public int StageID;
            public long Exp;
            public bool ActionPointNotEnough;
            public bool ShouldExit;
            public bool ShouldRepeat;
            public int ClearedWaveNumber;
            public int ActionPoint;
            public int LastClearedStageId;

            public IReadOnlyList<CountableItem> Rewards => _rewards;

            public void AddReward(CountableItem reward)
            {
                var sameReward =
                    _rewards.FirstOrDefault(e => e.ItemBase.Value.Equals(reward.ItemBase.Value));
                if (sameReward is null)
                {
                    _rewards.Add(reward);
                    return;
                }

                sameReward.Count.Value += reward.Count.Value;
            }
        }

        [Serializable]
        public struct RewardsArea
        {
            public GameObject root;
            public BattleReward[] rewards;
        }

        [Serializable]
        public struct DefeatTextArea
        {
            public GameObject root;
            public TextMeshProUGUI defeatText;
            public TextMeshProUGUI expText;
        }

        private const int Timer = 10;
        private static readonly Vector3 VfxBattleWinOffset = new Vector3(-0.05f, 1.2f, 10f);

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private GameObject victoryImageContainer = null;

        [SerializeField]
        private GameObject defeatImageContainer = null;

        [SerializeField]
        private TextMeshProUGUI worldStageId = null;

        [SerializeField]
        private GameObject topArea = null;

        [SerializeField]
        private DefeatTextArea defeatTextArea = default;

        [SerializeField]
        private RewardsArea rewardsArea = default;

        [SerializeField]
        private TextMeshProUGUI bottomText = null;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private TextMeshProUGUI closeButtonText = null;

        [SerializeField]
        private Button submitButton = null;

        [SerializeField]
        private TextMeshProUGUI submitButtonText = null;

        [SerializeField]
        private StageProgressBar stageProgressBar = null;

        [SerializeField]
        private GameObject[] victoryResultTexts = null;

        [SerializeField]
        private ActionPoint actionPoint = null;

        private BattleWin01VFX _battleWin01VFX;

        private BattleWin02VFX _battleWin02VFX;

        private BattleWin03VFX _battleWin03VFX;

        private Coroutine _coUpdateBottomText;

        private readonly WaitForSeconds _battleWinVFXYield = new WaitForSeconds(0.2f);

        private Animator _victoryImageAnimator;

        private bool _IsAlreadyOut = false;

        public Model SharedModel { get; private set; }

        public StageProgressBar StageProgressBar => stageProgressBar;

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (States.Instance.CurrentAvatarState.worldInformation
                        .TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
                    {
                        var canExit = world.StageClearedId >= Battle.RequiredStageForExitButton;
                        if (canExit)
                        {
                            StartCoroutine(OnClickClose());

                        }
                    }
                })
                .AddTo(gameObject);
            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    StartCoroutine(OnClickSubmit());
                })
                .AddTo(gameObject);

            CloseWidget = closeButton.onClick.Invoke;
            SubmitWidget = submitButton.onClick.Invoke;
            defeatTextArea.root.SetActive(false);
            defeatTextArea.defeatText.text =
                L10nManager.Localize("UI_BATTLE_RESULT_DEFEAT_MESSAGE");

            _victoryImageAnimator = victoryImageContainer.GetComponent<Animator>();
        }

        private IEnumerator OnClickClose()
        {
            _IsAlreadyOut = true;
            AudioController.PlayClick();
            if (SharedModel.State == BattleLog.Result.Win)
            {
                yield return CoDialog(SharedModel.StageID);
            }
            GoToMain();
            AnalyticsManager.Instance.BattleLeave();
        }

        private IEnumerator OnClickSubmit()
        {
            if (_IsAlreadyOut)
            {
                yield break;
            }

            AudioController.PlayClick();
            yield return CoRepeatCurrentOrProceedNextStage();
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName
                .ClickBattleResultNext);
        }

        private IEnumerator CoDialog(int worldStage)
        {
            var stageDialogs = Game.Game.instance.TableSheets.StageDialogSheet.Values
                .Where(i => i.StageId == worldStage)
                .OrderBy(i => i.DialogId)
                .ToArray();
            if (!stageDialogs.Any())
            {
                yield break;
            }

            var dialog = Widget.Find<Dialog>();

            foreach (var stageDialog in stageDialogs)
            {
                dialog.Show(stageDialog.DialogId);
                yield return new WaitWhile(() => dialog.gameObject.activeSelf);
            }
        }

        public void Show(Model model)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            SharedModel = model;
            _IsAlreadyOut = false;

            worldStageId.text = $"{SharedModel.WorldName} {StageInformation.GetStageIdString(SharedModel.StageID)}";
            actionPoint.SetActionPoint(model.ActionPoint);
            actionPoint.SetEventTriggerEnabled(true);

            foreach (var reward in rewardsArea.rewards)
            {
                reward.gameObject.SetActive(false);
            }

            base.Show();
            closeButton.gameObject.SetActive(model.StageID >= 3 || model.LastClearedStageId >= 3);
            UpdateView();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopVFX();

            foreach (var obj in victoryResultTexts)
            {
                obj.SetActive(false);
            }

            stageProgressBar.Close();
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateView()
        {
            switch (SharedModel.State)
            {
                case BattleLog.Result.Win:
                    StartCoroutine(CoUpdateViewAsVictory());
                    break;
                case BattleLog.Result.Lose:
                    UpdateViewAsDefeat(SharedModel.State);
                    break;
                case BattleLog.Result.TimeOver:
                    if (SharedModel.ClearedWaveNumber > 0)
                    {
                        StartCoroutine(CoUpdateViewAsVictory());
                    }
                    else
                    {
                        UpdateViewAsDefeat(SharedModel.State);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator CoUpdateViewAsVictory()
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
            StartCoroutine(EmitBattleWinVFX());
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleWin);

            victoryImageContainer.SetActive(true);
            _victoryImageAnimator.SetInteger("ClearedWave", SharedModel.ClearedWaveNumber);

            defeatImageContainer.SetActive(false);
            topArea.SetActive(true);
            defeatTextArea.root.SetActive(false);
            closeButton.interactable = true;
            closeButtonText.text = L10nManager.Localize("UI_MAIN");
            stageProgressBar.Show();

            _coUpdateBottomText = StartCoroutine(CoUpdateBottomText(Timer));
            yield return StartCoroutine(CoUpdateRewards());
        }

        private IEnumerator EmitBattleWinVFX()
        {
            yield return _battleWinVFXYield;
            AudioController.instance.PlaySfx(AudioController.SfxCode.Win);
            switch (SharedModel.ClearedWaveNumber)
            {
                case 1:
                    _battleWin01VFX =
                        VFXController.instance.CreateAndChase<BattleWin01VFX>(
                            ActionCamera.instance.transform,
                            VfxBattleWinOffset);
                    break;
                case 2:
                    _battleWin02VFX =
                        VFXController.instance.CreateAndChase<BattleWin02VFX>(
                            ActionCamera.instance.transform,
                            VfxBattleWinOffset);
                    break;
                case 3:
                    _battleWin03VFX =
                        VFXController.instance.CreateAndChase<BattleWin03VFX>(
                            ActionCamera.instance.transform,
                            VfxBattleWinOffset);
                    break;
            }
        }

        private void UpdateViewAsDefeat(BattleLog.Result result)
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleLose);

            victoryImageContainer.SetActive(false);
            defeatImageContainer.SetActive(true);
            topArea.SetActive(false);
            defeatTextArea.root.SetActive(true);
            var key = "UI_BATTLE_RESULT_DEFEAT_MESSAGE";
            if (result == BattleLog.Result.TimeOver)
            {
                key = "UI_BATTLE_RESULT_TIMEOUT_MESSAGE";
            }

            defeatTextArea.defeatText.text = L10nManager.Localize(key);
            defeatTextArea.expText.text = $"EXP + {SharedModel.Exp}";
            bottomText.enabled = false;
            closeButton.interactable = true;
            closeButtonText.text = L10nManager.Localize("UI_MAIN");
            submitButtonText.text = L10nManager.Localize("UI_BATTLE_AGAIN");

            _coUpdateBottomText = StartCoroutine(CoUpdateBottomText(Timer));
            StartCoroutine(CoUpdateRewards());
        }

        private IEnumerator CoUpdateRewards()
        {
            rewardsArea.root.SetActive(true);
            for (var i = 0; i < rewardsArea.rewards.Length; i++)
            {
                var view = rewardsArea.rewards[i];
                view.StartShowAnimation();
                var cleared = SharedModel.ClearedWaveNumber > i;
                switch (i)
                {
                    case 0:
                        view.Set(SharedModel.Exp, cleared);
                        break;
                    case 1:
                        view.Set(SharedModel.Rewards, Game.Game.instance.Stage.stageId, cleared);
                        break;
                    case 2:
                        view.Set(SharedModel.State == BattleLog.Result.Win && cleared);
                        break;
                }

                yield return new WaitForSeconds(0.5f);

                view.gameObject.SetActive(true);
                view.EnableStar(cleared);
                yield return null;
                AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            }

            yield return new WaitForSeconds(0.5f);

            foreach (var reward in rewardsArea.rewards)
            {
                reward.StopShowAnimation();
                reward.StartScaleTween();
            }
        }

        private IEnumerator CoUpdateBottomText(int limitSeconds)
        {
            var secondsFormat = L10nManager.Localize("UI_AFTER_N_SECONDS");
            string fullFormat;
            submitButton.gameObject.SetActive(false);
            SubmitWidget = closeButton.onClick.Invoke;
            if (SharedModel.ActionPointNotEnough)
            {
                fullFormat =
                    L10nManager.Localize("UI_BATTLE_RESULT_NOT_ENOUGH_ACTION_POINT_FORMAT");
            }
            else if (SharedModel.ShouldExit)
            {
                fullFormat = L10nManager.Localize("UI_BATTLE_EXIT_FORMAT");
            }
            else
            {
                fullFormat = SharedModel.ShouldRepeat
                    ? L10nManager.Localize("UI_BATTLE_RESULT_REPEAT_STAGE_FORMAT")
                    : L10nManager.Localize("UI_BATTLE_RESULT_NEXT_STAGE_FORMAT");
                submitButton.interactable = true;
                SubmitWidget = submitButton.onClick.Invoke;
                submitButtonText.text = SharedModel.ShouldRepeat
                    ? L10nManager.Localize("UI_BATTLE_AGAIN")
                    : L10nManager.Localize("UI_NEXT_STAGE");

                if (SharedModel.StageID == 3 &&
                    SharedModel.LastClearedStageId == 3 &&
                    SharedModel.State == BattleLog.Result.Win)
                {
                    submitButton.gameObject.SetActive(false);
                    bottomText.text = string.Empty;

                    yield break;
                }

                submitButton.gameObject.SetActive(true);
            }

            bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));

            yield return new WaitUntil(() => CanClose);

            var floatTime = (float) limitSeconds;
            var floatTimeMinusOne = limitSeconds - 1f;
            while (limitSeconds > 0)
            {
                yield return null;

                floatTime -= Time.deltaTime;
                if (floatTimeMinusOne < floatTime)
                {
                    continue;
                }

                limitSeconds--;
                bottomText.text =
                    string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));
                floatTimeMinusOne = limitSeconds - 1f;
            }

            if (SharedModel.ActionPointNotEnough || SharedModel.ShouldExit)
            {
                GoToMain();
            }
            else
            {
                StartCoroutine(OnClickSubmit());
            }
        }

        private IEnumerator CoRepeatCurrentOrProceedNextStage()
        {
            if (!submitButton.interactable)
                yield break;

            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            var isNext = !SharedModel.ShouldRepeat;

            closeButton.interactable = false;
            submitButton.interactable = false;
            actionPoint.SetEventTriggerEnabled(false);

            StopCoUpdateBottomText();
            StartCoroutine(CoFadeOut());
            var stage = Game.Game.instance.Stage;
            var stageLoadingScreen = Find<StageLoadingScreen>();
            stageLoadingScreen.Show(stage.zone,
                SharedModel.WorldName,
                isNext ? SharedModel.StageID + 1: SharedModel.StageID,
                isNext, SharedModel.StageID);
            Find<Status>().Close();

            StopVFX();
            var player = stage.RunPlayerForNextStage();
            player.DisableHUD();

            var worldId = stage.worldId;
            var stageId = SharedModel.ShouldRepeat
                ? stage.stageId
                : stage.stageId + 1;
            ActionRenderHandler.Instance.Pending = true;
            var props = new Value
            {
                ["StageId"] = stageId,
            };
            var eventKey = "Next Stage";
            if (SharedModel.ShouldRepeat)
            {
                eventKey = SharedModel.ClearedWaveNumber == 3 ? "Repeat" : "Retry";
            }

            var eventName = $"Unity/Stage Exit {eventKey}";
            Mixpanel.Track(eventName, props);
            yield return Game.Game.instance.ActionManager
                .HackAndSlash(
                    player.Costumes,
                    player.Equipments,
                    new List<Consumable>(),
                    worldId,
                    stageId)
                .Subscribe(_ => { },
                    e => ActionRenderHandler.BackToMain(false, e));
        }

        public void NextStage(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            Debug.Log("NextStage From ResponseHackAndSlash");
            StartCoroutine(CoGoToNextStageClose(eval));
        }

        private IEnumerator CoGoToNextStageClose(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            yield return StartCoroutine(Find<StageLoadingScreen>().CoClose());
            yield return StartCoroutine(CoFadeOut());
            Game.Event.OnStageStart.Invoke(eval.Action.Result);
            Close();
        }

        public void NextMimisbrunnrStage(ActionBase.ActionEvaluation<MimisbrunnrBattle> eval)
        {
            Debug.Log("NextStage From ResponseHackAndSlash");
            StartCoroutine(CoGoToNextMimisbrunnrStageClose(eval));
        }
        private IEnumerator CoGoToNextMimisbrunnrStageClose(ActionBase.ActionEvaluation<MimisbrunnrBattle> eval)
        {
            if (Find<Menu>().IsActive())
            {
                yield break;
            }

            yield return StartCoroutine(Find<StageLoadingScreen>().CoClose());
            yield return StartCoroutine(CoFadeOut());
            Game.Event.OnStageStart.Invoke(eval.Action.Result);
            Close();
        }

        public void GoToMain()
        {
            var props = new Value
            {
                ["StageId"] = Game.Game.instance.Stage.stageId,
            };
            var eventKey = Game.Game.instance.Stage.isExitReserved ? "Quit" : "Main";
            var eventName = $"Unity/Stage Exit {eventKey}";
            Mixpanel.Track(eventName, props);

            Find<Battle>().Close();
            Game.Event.OnRoomEnter.Invoke(true);
            Close();
        }

        private void StopCoUpdateBottomText()
        {
            if (_coUpdateBottomText != null)
            {
                StopCoroutine(_coUpdateBottomText);
            }
        }

        private IEnumerator CoFadeOut()
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime;

                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void StopVFX()
        {
            if (_battleWin01VFX)
            {
                _battleWin01VFX.Stop();
                _battleWin01VFX = null;
            }

            if (_battleWin02VFX)
            {
                _battleWin02VFX.Stop();
                _battleWin02VFX = null;
            }

            if (_battleWin03VFX)
            {
                _battleWin03VFX.Stop();
                _battleWin03VFX = null;
            }

            foreach (var reward in rewardsArea.rewards)
            {
                reward.StopVFX();
            }
        }
    }
}
