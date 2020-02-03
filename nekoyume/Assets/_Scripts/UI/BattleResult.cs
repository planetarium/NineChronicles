using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Manager;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
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
            public long Exp;
            public bool ActionPointNotEnough;
            public bool ShouldExit;
            public bool ShouldRepeat;

            public IReadOnlyList<CountableItem> Rewards => _rewards;

            public void AddReward(CountableItem reward)
            {
                var sameReward = _rewards.FirstOrDefault(e => e.ItemBase.Value.Equals(reward.ItemBase.Value));
                if (sameReward is null)
                {
                    _rewards.Add(reward);
                    return;
                }

                sameReward.Count.Value += reward.Count.Value;
            }
        }

        [Serializable]
        public struct TopArea 
        {
            public TextMeshProUGUI topText;
            public GameObject expContainer;
            public TextMeshProUGUI expText;
            public TextMeshProUGUI expValueText;
        }

        [Serializable]
        public struct RewardsArea
        {
            public GameObject root;
            public TextMeshProUGUI text;
            public SimpleCountableItemView[] rewards;
        }

        [Serializable]
        public struct SuggestionsArea
        {
            public GameObject root;
            public TextMeshProUGUI text1;
            public TextMeshProUGUI text2;
            public Button submitButton1;
            public Button submitButton2;
            public TextMeshProUGUI submitButtonText1;
            public TextMeshProUGUI submitButtonText2;
        }

        private const int Timer = 10;
        private static readonly Vector3 VfxBattleWin01Offset = new Vector3(-3.43f, -0.28f, 10f);
        private static readonly Vector3 VfxBattleWin02Offset = new Vector3(0.0f, 0.85f, 10f);

        public CanvasGroup canvasGroup;
        public GameObject victoryImageContainer;
        public GameObject defeatImageContainer;
        public TopArea topArea;
        public RewardsArea rewardsArea;
        public SuggestionsArea suggestionsArea;
        public TextMeshProUGUI bottomText;
        public Button closeButton;
        public TextMeshProUGUI closeButtonText;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;

        private BattleWin01VFX _battleWin01VFX;
        private BattleWin02VFX _battleWin02VFX;
        private Coroutine _coUpdateBottomText;
        private readonly WaitForSeconds _battleWin02VFXYield = new WaitForSeconds(0.55f);

        public Model SharedModel { get; private set; }

        public Subject<bool> BattleEndedSubject = new Subject<bool>();
        public IDisposable battleEndedStream;

        protected override void Awake()
        {
            base.Awake();

            topArea.expText.text = LocalizationManager.Localize("UI_EXP");
            rewardsArea.text.text = LocalizationManager.Localize("UI_ADDITIONAL_REWARDS");
            suggestionsArea.text1.text = LocalizationManager.Localize("UI_BATTLE_RESULT_DEFEAT_SUGGESTION_1");
            suggestionsArea.text2.text = LocalizationManager.Localize("UI_BATTLE_RESULT_DEFEAT_SUGGESTION_2");
            suggestionsArea.submitButtonText1.text =
                LocalizationManager.Localize("UI_BATTLE_RESULT_DEFEAT_SUGGESTION_1_BUTTON");
            suggestionsArea.submitButtonText2.text =
                LocalizationManager.Localize("UI_BATTLE_RESULT_DEFEAT_SUGGESTION_2_BUTTON");

            suggestionsArea.submitButton1.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToWorldMap();
                    AnalyticsManager.Instance.BattleLeave();
                })
                .AddTo(gameObject);
            suggestionsArea.submitButton2.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToCraftShop();
                    AnalyticsManager.Instance.BattleLeave();
                })
                .AddTo(gameObject);
            closeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    GoToMain();
                    AnalyticsManager.Instance.BattleLeave();
                })
                .AddTo(gameObject);
            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    StartCoroutine(CoRepeatCurrentOrProceedNextStage());
                    AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickBattleResultNext);
                })
                .AddTo(gameObject);
        }

        public void Show(Model model)
        {
            base.Show();

            BattleEndedSubject.OnNext(IsActive());
            canvasGroup.alpha = 1f;
            SharedModel = model;
            UpdateView();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            battleEndedStream.Dispose();
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
                case BattleLog.Result.TimeOver:
                    UpdateViewAsDefeat(SharedModel.State);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator CoUpdateViewAsVictory()
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
            _battleWin01VFX =
                VFXController.instance.Create<BattleWin01VFX>(ActionCamera.instance.transform, VfxBattleWin01Offset);
            StartCoroutine(EmitBattleWin02VFX());
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleWin);

            victoryImageContainer.SetActive(true);
            defeatImageContainer.SetActive(false);
            topArea.topText.text = LocalizationManager.Localize("UI_BATTLE_RESULT_VICTORY_MESSAGE");
            topArea.expValueText.text = SharedModel.Exp.ToString();
            topArea.expContainer.SetActive(true);
            suggestionsArea.root.SetActive(false);
            bottomText.enabled = false;
            closeButton.interactable = true;
            closeButtonText.text = LocalizationManager.Localize("UI_MAIN");

            if (SharedModel.ActionPointNotEnough || SharedModel.ShouldExit)
            {
                submitButton.gameObject.SetActive(false);
            }
            else
            {
                submitButton.interactable = true;

                submitButtonText.text = SharedModel.ShouldRepeat
                    ? LocalizationManager.Localize("UI_BATTLE_AGAIN")
                    : LocalizationManager.Localize("UI_NEXT_STAGE");
                submitButton.gameObject.SetActive(true);
            }

            yield return StartCoroutine(CoUpdateRewards());

            _coUpdateBottomText = StartCoroutine(CoUpdateBottomText(Timer));
        }

        private IEnumerator EmitBattleWin02VFX()
        {
            yield return _battleWin02VFXYield;
            AudioController.instance.PlaySfx(AudioController.SfxCode.Win);
            _battleWin02VFX =
                VFXController.instance.Create<BattleWin02VFX>(ActionCamera.instance.transform, VfxBattleWin02Offset);
        }

        private void UpdateViewAsDefeat(BattleLog.Result result)
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleLose);

            SetShouldRepeatFalse();

            victoryImageContainer.SetActive(false);
            defeatImageContainer.SetActive(true);
            var key = "UI_BATTLE_RESULT_DEFEAT_MESSAGE";
            if (result == BattleLog.Result.TimeOver)
            {
                key = "UI_BATTLE_RESULT_TIMEOUT_MESSAGE";
            }
            topArea.topText.text = LocalizationManager.Localize(key);
            topArea.expContainer.SetActive(false);
            suggestionsArea.root.SetActive(true);
            suggestionsArea.submitButton1.interactable = true;
            suggestionsArea.submitButton2.interactable = true;
            bottomText.enabled = false;
            closeButton.interactable = true;
            closeButtonText.text = LocalizationManager.Localize("UI_MAIN");
            submitButton.gameObject.SetActive(false);

            StartCoroutine(CoUpdateRewards());
        }

        private IEnumerator CoUpdateRewards()
        {
            if (SharedModel.Rewards.Count > 0)
            {
                rewardsArea.root.SetActive(true);

                foreach (var view in rewardsArea.rewards)
                {
                    view.gameObject.SetActive(false);
                }

                for (var i = 0; i < rewardsArea.rewards.Length; i++)
                {
                    var view = rewardsArea.rewards[i];
                    if (SharedModel.Rewards.Count <= i)
                    {
                        break;
                    }

                    yield return new WaitForSeconds(0.5f);

                    var model = SharedModel.Rewards[i];
                    view.SetData(model);
                    view.gameObject.SetActive(true);
                    yield return null;
                    VFXController.instance.Create<DropItemInventoryVFX>(view.transform, view.CenterOffsetAsPosition);
                    AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
                }

                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                rewardsArea.root.SetActive(false);
            }
        }

        private IEnumerator CoUpdateBottomText(int limitSeconds)
        {
            var secondsFormat = LocalizationManager.Localize("UI_AFTER_N_SECONDS");
            string fullFormat;
            if (SharedModel.ActionPointNotEnough)
            {
                fullFormat = LocalizationManager.Localize("UI_BATTLE_RESULT_NOT_ENOUGH_ACTION_POINT_FORMAT");
            }
            else if (SharedModel.ShouldExit)
            {
                fullFormat = LocalizationManager.Localize("UI_BATTLE_EXIT_FORMAT");
            }
            else
            {
                fullFormat = SharedModel.ShouldRepeat
                    ? LocalizationManager.Localize("UI_BATTLE_RESULT_REPEAT_STAGE_FORMAT")
                    : LocalizationManager.Localize("UI_BATTLE_RESULT_NEXT_STAGE_FORMAT");
            }


            bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));
            bottomText.enabled = true;

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
                bottomText.text = string.Format(fullFormat, string.Format(secondsFormat, limitSeconds));
                floatTimeMinusOne = limitSeconds - 1f;
            }

            if (SharedModel.ActionPointNotEnough || SharedModel.ShouldExit)
            {
                GoToMain();
            }
            else
            {
                StartCoroutine(CoRepeatCurrentOrProceedNextStage());
            }
        }

        private IEnumerator CoRepeatCurrentOrProceedNextStage()
        {
            if (!submitButton.interactable)
                yield break;
            
            closeButton.interactable = false;
            submitButton.interactable = false;
            suggestionsArea.submitButton1.interactable = false;
            suggestionsArea.submitButton2.interactable = false;

            StopCoUpdateBottomText();
            StartCoroutine(CoFadeOut());
            var stage = Game.Game.instance.Stage;
            var stageLoadingScreen = Find<StageLoadingScreen>();
            stageLoadingScreen.Show(stage.zone);
            Find<Status>().Close();

            if (_battleWin01VFX)
            {
                _battleWin01VFX.Stop();
            }

            var player = stage.RunPlayer();
            player.DisableHUD();

            var worldId = stage.worldId;
            var stageId = SharedModel.ShouldRepeat
                ? stage.stageId
                : stage.stageId + 1;
            yield return ActionManager.instance
                .HackAndSlash(player.Equipments, new List<Consumable>(), worldId, stageId)
                .Subscribe(_ => { }, (_) => Find<ActionFailPopup>().Show("Action timeout during HackAndSlash."));
        }

        public void NextStage(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            Debug.Log("NextStage From ResponseHackAndSlash");
            StartCoroutine(CoGoToNextStageClose(eval));
        }

        private IEnumerator CoGoToNextStageClose(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            yield return StartCoroutine(Find<StageLoadingScreen>().CoClose());
            yield return StartCoroutine(CoFadeOut());
            Game.Event.OnStageStart.Invoke(eval.Action.Result);
            Close();
        }

        public void GoToMain()
        {
            SetShouldRepeatFalse();

            _battleWin01VFX?.Stop();
            _battleWin02VFX?.Stop();


            Find<Battle>().Close();
            Game.Event.OnRoomEnter.Invoke();
            Close();
        }

        private void GoToWorldMap()
        {
            SetShouldRepeatFalse();

            _battleWin01VFX?.Stop();
            _battleWin02VFX?.Stop();
        }

        private void GoToCraftShop()
        {
            SetShouldRepeatFalse();

            _battleWin01VFX?.Stop();
            _battleWin02VFX?.Stop();
        }

        private void SetShouldRepeatFalse()
        {
            var stage = Game.Game.instance.Stage;
            stage.repeatStage = false;
            SharedModel.ShouldRepeat = false;
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

            _battleWin02VFX.Stop();
            canvasGroup.alpha = 0f;
        }
    }
}
