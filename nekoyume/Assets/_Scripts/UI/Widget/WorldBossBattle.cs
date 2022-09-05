using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class WorldBossBattle : Widget
    {
        [SerializeField]
        private RaidBossStatus bossStatus;

        [SerializeField]
        private RaidPlayerStatus playerStatus;

        [SerializeField]
        private ComboText comboText;

        [SerializeField]
        private RaidProgressBar progressBar;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
        }

        public void Show(
            int bossId,
            Game.Character.Player player,
            bool ignoreShowAnimation = false)
        {
            comboText.comboMax = AttackCountHelper.GetCountMax(player.Level);
            comboText.Close();

            var turnLimit = 150;
            var sheet = Game.Game.instance.TableSheets.WorldBossCharacterSheet;
            if (sheet.TryGetValue(bossId, out var boss))
            {
                turnLimit = boss.WaveStats.FirstOrDefault().TurnLimit;
            }

            playerStatus.SetData(player, turnLimit);
            progressBar.Show(bossId);
            base.Show(ignoreShowAnimation);
        }

        public void UpdateScore(int score)
        {
            progressBar.UpdateScore(score);
        }

        public void OnWaveCompleted()
        {
            progressBar.CompleteWave();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            bossStatus.Close(ignoreCloseAnimation);
            progressBar.Close();
            base.Close(ignoreCloseAnimation);
        }

        public void SetBossProfile(Enemy enemy, int turnLimit)
        {
            bossStatus.SetProfile(enemy);
            bossStatus.Show();
            playerStatus.UpdateTurnLimit(turnLimit);
        }

        public void UpdateStatus(
            int currentHp,
            int maxHp,
            Dictionary<int, Nekoyume.Model.Buff.Buff> buffs)
        {
            bossStatus.SetHp(currentHp, maxHp);
            bossStatus.SetBuff(buffs);
        }

        public void ShowComboText(bool attacked)
        {
            comboText.StopAllCoroutines();
            comboText.Show(attacked);
        }
    }
}
