using Nekoyume.Game;
using Nekoyume.Model;
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

        public void Show(BattleLog.Result result)
        {
            base.Show();

            var win = result == BattleLog.Result.Win;
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
        }

        public void BackToRanking()
        {
            Game.Game.instance.Stage.objectPool.ReleaseAll();
            ActionCamera.instance.SetPoint(0f, 0f);
            ActionCamera.instance.Idle();
            Find<RankingBoard>().Show();
            Close();
        }
    }
}
