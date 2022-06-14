using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class RankingBattleResultPopup : PopupWidget
    {
        [SerializeField] private GameObject victoryImageContainer = null;

        [SerializeField] private GameObject defeatImageContainer = null;

        [SerializeField] private TextButton submitButton = null;

        [SerializeField] private TextMeshProUGUI scoreText = null;

        [SerializeField] private List<SimpleCountableItemView> rewards = null;

        private static readonly Vector3 VfxBattleWinOffset = new Vector3(-0.05f, .25f, 10f);

        private System.Action _onClose;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
            SubmitWidget = BackToRanking;
            submitButton.OnClick = BackToRanking;
        }

        public void Show(ArenaLog log, IReadOnlyList<ItemBase> rewardItems, System.Action onClose)
        {
            base.Show();

            var win = log.Result == ArenaLog.ArenaResult.Win;
            var code = win ? AudioController.MusicCode.PVPWin : AudioController.MusicCode.PVPLose;
            AudioController.instance.PlayMusic(code);
            victoryImageContainer.SetActive(win);
            defeatImageContainer.SetActive(!win);
            if (win)
            {
                VFXController.instance.CreateAndChase<PVPVictoryVFX>(
                    ActionCamera.instance.transform, VfxBattleWinOffset);
            }

            scoreText.text = $"{log.Score}";

            var items = rewardItems.Select(e => new CountableItem(e, 1)).ToList();
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

            _onClose = onClose;
        }

        private void BackToRanking()
        {
            Close();
            _onClose?.Invoke();
        }
    }
}
