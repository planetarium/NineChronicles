using System;
using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using Nekoyume.Manager;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BattleResult : PopupWidget
    {
        public class Model
        {
            public BattleLog.Result state;
            public long exp;
            public readonly List<CountableItem> rewards = new List<CountableItem>();
            public bool shouldRepeat;
        }

        [Serializable]
        public struct TopArea
        {
            public Text topText;
            public GameObject expContainer;
            public Text expText;
            public Text expValueText;
        }

        [Serializable]
        public struct RewardsArea
        {
            public GameObject root;
            public Text text;
            public SimpleCountableItemView[] rewards;
        }

        [Serializable]
        public struct SuggestionsArea
        {
            public GameObject root;
            public Text text1;
            public Text text2;
            public Button submitButton1;
            public Button submitButton2;
            public Text submitButtonText1;
            public Text submitButtonText2;
        }
        
        private static readonly Vector3 VfxBattleWinOffset = new Vector3(-3.43f, -0.28f, 10f);
        private const int Timer = 5;

        public CanvasGroup canvasGroup;
        public GameObject victoryImageContainer;
        public GameObject defeatImageContainer;
        public TopArea topArea;
        public RewardsArea rewardsArea;
        public SuggestionsArea suggestionsArea;
        public Text bottomText;
        public Button closeButton;
        public Text closeButtonText;
        public Button submitButton;
        public Text submitButtonText;

        private BattleWinVFX _battleWinVFX;
        private Coroutine _coUpdateBottomText;

        public Model SharedModel { get; private set; }

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
                    StartCoroutine(CoGoToNextStage());
                    AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickBattleResultNext);
                })
                .AddTo(gameObject);
        }

        public void Show(Model model)
        {
            base.Show();
            canvasGroup.alpha = 1f;
            SharedModel = model;
            UpdateView();
        }

        private void UpdateView()
        {
            switch (SharedModel.state)
            {
                case BattleLog.Result.Win:
                    UpdateViewAsVictory();
                    break;
                case BattleLog.Result.Lose:
                    UpdateViewAsDefeat();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateViewAsVictory()
        {
            victoryImageContainer.SetActive(true);
            defeatImageContainer.SetActive(false);
            topArea.topText.text = LocalizationManager.Localize("UI_BATTLE_RESULT_VICTORY_MESSAGE");
            topArea.expValueText.text = SharedModel.exp.ToString();
            topArea.expContainer.SetActive(true);
            if (SharedModel.rewards.Count > 0)
            {
                for (var i = 0; i < rewardsArea.rewards.Length; i++)
                {
                    var view = rewardsArea.rewards[i];
                    if (SharedModel.rewards.Count <= i)
                    {
                        view.gameObject.SetActive(false);

                        continue;
                    }

                    var model = SharedModel.rewards[i];
                    view.SetData(model);
                    view.gameObject.SetActive(true);
                }

                rewardsArea.root.SetActive(true);
            }
            else
            {
                rewardsArea.root.SetActive(false);
            }

            suggestionsArea.root.SetActive(false);
            bottomText.gameObject.SetActive(true);
            closeButton.interactable = true;
            closeButtonText.text = LocalizationManager.Localize("UI_MAIN");
            submitButton.interactable = true;
            submitButtonText.text = SharedModel.shouldRepeat
                ? LocalizationManager.Localize("UI_BATTLE_AGAIN")
                : LocalizationManager.Localize("UI_NEXT_STAGE");
            submitButton.gameObject.SetActive(true);

            _coUpdateBottomText = StartCoroutine(CoUpdateBottomText(Timer));
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
            _battleWinVFX =
                VFXController.instance.Create<BattleWinVFX>(ActionCamera.instance.transform, VfxBattleWinOffset);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleWin);
        }

        private void UpdateViewAsDefeat()
        {
            SetShouldRepeatFalse();
            
            victoryImageContainer.SetActive(false);
            defeatImageContainer.SetActive(true);
            topArea.topText.text = LocalizationManager.Localize("UI_BATTLE_RESULT_DEFEAT_MESSAGE");
            topArea.expContainer.SetActive(false);
            rewardsArea.root.SetActive(false);
            suggestionsArea.root.SetActive(true);
            suggestionsArea.submitButton1.interactable = true;
            suggestionsArea.submitButton2.interactable = true;
            bottomText.gameObject.SetActive(false);
            closeButton.interactable = true;
            closeButtonText.text = LocalizationManager.Localize("UI_EXIT");
            submitButton.gameObject.SetActive(false);
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleLose);
        }

        private IEnumerator CoUpdateBottomText(int limitSeconds)
        {
            var format = LocalizationManager.Localize("UI_AFTER_N_SECONDS");
            bottomText.text = string.Format(format, limitSeconds);
            
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
                bottomText.text = string.Format(format, limitSeconds);
                floatTimeMinusOne = limitSeconds - 1f;
            }

            StartCoroutine(CoGoToNextStage());
        }

        private IEnumerator CoGoToNextStage()
        {
            closeButton.interactable = false;
            submitButton.interactable = false;
            suggestionsArea.submitButton1.interactable = false;
            suggestionsArea.submitButton2.interactable = false;
            
            StopCoUpdateBottomText();
            var coFadeOut = StartCoroutine(CoFadeOut());
            
            var stage = Game.Game.instance.stage;
            var stageLoadingScreen = Find<StageLoadingScreen>();
            stageLoadingScreen.Show(stage.zone);
            Find<Status>().Close();
            Find<Gold>().Close();

            if (_battleWinVFX)
            {
                _battleWinVFX.Stop();
            }

            var player = stage.RunPlayer();
            player.DisableHUD();

            var stageId = SharedModel.shouldRepeat ? stage.id : stage.id + 1;
            yield return ActionManager.instance.HackAndSlash(player.equipments, new List<Food>(), stageId).ToYieldInstruction();
            yield return StartCoroutine(stageLoadingScreen.CoClose());
            yield return coFadeOut;
            Game.Event.OnStageStart.Invoke();
            Close();
        }
        
        private void GoToMain()
        {
            SetShouldRepeatFalse();
            
            if (_battleWinVFX)
            {
                _battleWinVFX.Stop();
            }
            
            Game.Event.OnRoomEnter.Invoke();
            Close();
        }

        private void GoToWorldMap()
        {
            SetShouldRepeatFalse();
            
            if (_battleWinVFX)
            {
                _battleWinVFX.Stop();
            }
        }

        private void GoToCraftShop()
        {
            SetShouldRepeatFalse();
            
            if (_battleWinVFX)
            {
                _battleWinVFX.Stop();
            }
        }

        private void SetShouldRepeatFalse()
        {
            var stage = Game.Game.instance.stage;
            stage.repeatStage = false;
            SharedModel.shouldRepeat = false;
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
        }
    }
}
