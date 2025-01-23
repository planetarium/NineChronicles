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
            if (battleResponse != null)
            {
                var scoreChange = battleResponse.MyScoreChange.Value;
                var scoreChangeColor = scoreChange > 0 ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                var scoreChangeSign = scoreChange > 0 ? "+" : "-";
                scoreText.text = $"{battleResponse.MyScore - scoreChange} <color=#{ColorUtility.ToHtmlStringRGB(scoreChangeColor)}>{scoreChangeSign}{Math.Abs(scoreChange)}</color>";
            }
            else
            {
                scoreText.text = $"{log.Score}";
            }
            winLoseCountText.text = winDefeatCount.HasValue
                ? $"Win {winDefeatCount.Value.win} Lose {winDefeatCount.Value.defeat}"
                : string.Empty;
            winLoseCountText.gameObject.SetActive(winDefeatCount.HasValue);

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
