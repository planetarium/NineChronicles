using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
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
        public List<SimpleCountableItemView> rewards;

        protected override void Awake()
        {
            base.Awake();
            submitButtonText.text = L10nManager.Localize("UI_BACK_TO_ARENA");

            CloseWidget = null;
            SubmitWidget = BackToRanking;
        }

        public void Show(BattleLog log, IReadOnlyList<CountableItem> reward)
        {
            base.Show();

            var win = log.result == BattleLog.Result.Win;
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
            scoreText.text = $"{log.score}";
            for (var i = 0; i < rewards.Count; i++)
            {
                var view = rewards[i];
                view.gameObject.SetActive(false);
                if (i < reward.Count)
                {
                    view.SetData(reward[i]);
                    view.gameObject.SetActive(true);
                }
            }
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
