using System.Collections.Generic;
using Nekoyume.ApiClient;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using GeneratedApiNamespace.ArenaServiceClient;
using System;
using Nekoyume.State;

namespace Nekoyume.UI
{
    public class RankingBattleResultPopup : PopupWidget
    {
        [SerializeField]
        private GameObject victoryImageContainer = null;

        [SerializeField]
        private GameObject defeatImageContainer = null;

        [SerializeField]
        private TextButton submitButton = null;

        [SerializeField]
        private TextMeshProUGUI scoreText = null;

        [SerializeField]
        private TextMeshProUGUI winLoseCountText = null;

        [SerializeField]
        private List<SimpleCountableItemView> rewards = null;

        [SerializeField]
        private GameObject[] seasonPassObjs;

        [SerializeField]
        private TextMeshProUGUI seasonPassCourageAmount;

        [SerializeField]
        private GameObject medalItemView;

        private static readonly Vector3 VfxBattleWinOffset = new(-0.05f, .25f, 10f);

        private System.Action _onClose;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
            SubmitWidget = BackToRanking;
            submitButton.OnClick = BackToRanking;
        }

        public void Show(
            ArenaLog log,
            IReadOnlyList<ItemBase> rewardItems,
            System.Action onClose,
            (int win, int defeat)? winDefeatCount = null,
            BattleResponse battleResponse = null)
        {
            base.Show();

            var win = log.Result == ArenaLog.ArenaResult.Win;
            var code = win
                ? AudioController.MusicCode.PVPWin
                : AudioController.MusicCode.PVPLose;
            AudioController.instance.PlayMusic(code);
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
            if (win)
            {
                VFXController.instance.CreateAndChase<PVPVictoryVFX>(
                    ActionCamera.instance.transform, VfxBattleWinOffset);
            }

            NcDebug.Log($"battleLogResponse: {battleResponse}");
            NcDebug.Log($"log.Score: {log.Score}");
            if (battleResponse != null && battleResponse.MyScoreChange.HasValue && battleResponse.MyScore.HasValue)
            {
                try
                {
                    var scoreChange = battleResponse.MyScoreChange.Value;
                    var scoreChangeColor = scoreChange > 0 ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                    var scoreChangeSign = scoreChange > 0 ? "+" : "-";
                    scoreText.text = $"{battleResponse.MyScore - scoreChange} <color=#{ColorUtility.ToHtmlStringRGB(scoreChangeColor)}>{scoreChangeSign}{Math.Abs(scoreChange)}</color>";
                }
                catch (Exception e)
                {
                    NcDebug.LogError($"Error occurred: {e.Message}");
                    scoreText.text = $"{log.Score}";    
                }
            }
            else
            {
                //폴링 실패하여 정보를 가져올수없는상태라 마지막 아래나 배틀진입 할때의 정보를가지고 임시로 보여준다.
                var info = Find<ArenaBattlePreparation>().GetCurrentOpponentInfo();
                if (info != null)
                {
                    try
                    {
                        var scoreChange = win ? info.ScoreGainOnWin : info.ScoreLossOnLose;
                        var scoreChangeColor = scoreChange > 0 ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                        var scoreChangeSign = scoreChange > 0 ? "+" : "-";
                        scoreText.text = $"{battleResponse.MyScore - scoreChange} <color=#{ColorUtility.ToHtmlStringRGB(scoreChangeColor)}>{scoreChangeSign}{Math.Abs(scoreChange)}</color>";
                    }
                    catch (Exception e)
                    {
                        NcDebug.LogError($"Error occurred: {e.Message}");
                        scoreText.text = $"{log.Score}";
                    }
                }
                else
                {
                    NcDebug.LogError($"Failed to retrieve information. Score: {log.Score}");
                    scoreText.text = $"{log.Score}";
                }
            }
            winLoseCountText.text = winDefeatCount.HasValue
                ? $"Win {winDefeatCount.Value.win} Lose {winDefeatCount.Value.defeat}"
                : string.Empty;
            winLoseCountText.gameObject.SetActive(winDefeatCount.HasValue);

            var currentSeason = RxProps.GetSeasonResponseByBlockIndex(Game.Game.instance.Agent.BlockIndex);
            if (win && currentSeason != null && currentSeason.ArenaType != ArenaType.SEASON)
            {
                medalItemView.SetActive(true);
            }
            else
            {
                medalItemView.SetActive(false);
            }

            var items = rewardItems.ToCountableItems();
            for (var i = 0; i < rewards.Count; i++)
            {
                var view = rewards[i];
                view.gameObject.SetActive(false);
                if (i < items.Count)
                {
                    view.SetData(items[i]);
                    view.gameObject.SetActive(true);
                }
            }

            var repeatCount = 1;
            if (winDefeatCount.HasValue)
            {
                repeatCount = winDefeatCount.Value.win + winDefeatCount.Value.defeat;
            }

            RefreshSeasonPassCourageAmount(repeatCount);

            _onClose = onClose;
        }

        private void BackToRanking()
        {
            Close();
            _onClose?.Invoke();
        }

        private void RefreshSeasonPassCourageAmount(int count)
        {
            var seasonPassServiceManager = ApiClients.Instance.SeasonPassServiceManager;
            if (seasonPassServiceManager.CurrentSeasonPassData != null)
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(true);
                }
                var expAmount = seasonPassServiceManager.ExpPointAmount(SeasonPassServiceClient.PassType.CouragePass, SeasonPassServiceClient.ActionType.battle_arena);
                seasonPassCourageAmount.text = $"+{expAmount * count}";
            }
            else
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(false);
                }
            }
        }
    }
}
