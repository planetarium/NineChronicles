using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RankingBattleResult : PopupWidget
    {
        public CanvasGroup canvasGroup;
        public GameObject victoryImageContainer;
        public GameObject defeatImageContainer;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;
        public TextMeshProUGUI scoreText;

        protected override void Awake()
        {
            base.Awake();
            submitButtonText.text = L10nManager.Localize("UI_BACK_TO_ARENA");

            CloseWidget = null;
            SubmitWidget = BackToRanking;
        }

        public void Show(BattleLog.Result result, int score, int diffScore)
        {
            base.Show();

            var win = result == BattleLog.Result.Win;
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
            scoreText.text = $"{score}";
        }

        public void BackToRanking()
        {
            Game.Game.instance.Stage.objectPool.ReleaseAll();
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();
            Find<RankingBoard>().Show();
            Close();
        }
    }
}
