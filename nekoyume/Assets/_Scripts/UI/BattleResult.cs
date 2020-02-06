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
            public int phase;

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
        public struct RewardsArea
        {
            public GameObject root;
            public BattleReward[] rewards;
        }

        private const int Timer = 10;
        private static readonly Vector3 VfxBattleWin01Offset = new Vector3(-3.43f, -0.28f, 10f);
        private static readonly Vector3 VfxBattleWin02Offset = new Vector3(0.0f, 0.85f, 10f);

        public CanvasGroup canvasGroup;
        public GameObject victoryImageContainer;
        public GameObject defeatImageContainer;
        public RewardsArea rewardsArea;
        public TextMeshProUGUI bottomText;
        public Button closeButton;
        public TextMeshProUGUI closeButtonText;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;
        public StageProgressBar stageProgressBar;

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

            CloseWidget = closeButton.onClick.Invoke;
            SubmitWidget = submitButton.onClick.Invoke;
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
            bottomText.enabled = false;
            closeButton.interactable = true;
            closeButtonText.text = LocalizationManager.Localize("UI_MAIN");
            stageProgressBar.Show();

            if (SharedModel.ActionPointNotEnough || SharedModel.ShouldExit)
            {
                submitButton.gameObject.SetActive(false);
                SubmitWidget = closeButton.onClick.Invoke;
            }
            else
            {
                submitButton.interactable = true;
                SubmitWidget = submitButton.onClick.Invoke;

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
            bottomText.enabled = false;
            closeButton.interactable = true;
            closeButtonText.text = LocalizationManager.Localize("UI_MAIN");
            submitButton.gameObject.SetActive(false);

            StartCoroutine(CoUpdateRewards());
        }

        private IEnumerator CoUpdateRewards()
        {
            rewardsArea.root.SetActive(true);
            for (var i = 0; i < rewardsArea.rewards.Length; i++)
            {
                var view = rewardsArea.rewards[i];
                view.gameObject.SetActive(false);
                if (i == 0)
                {
                    view.Set(SharedModel.Exp, SharedModel.phase >= i);
                }

                if (i == 1)
                {
                    view.Set(SharedModel.Rewards, SharedModel.phase >= i);
                }

                if (i == 2)
                {
                    view.Set(SharedModel.State == BattleLog.Result.Win && SharedModel.phase >= 2);
                }

                yield return new WaitForSeconds(0.5f);

                view.gameObject.SetActive(true);
                yield return null;
                AudioController.instance.PlaySfx(AudioController.SfxCode.RewardItem);
            }

            yield return new WaitForSeconds(0.5f);
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
