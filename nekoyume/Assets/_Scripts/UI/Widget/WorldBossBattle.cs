using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
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

        public void SetData(int bossId)
        {
            var turnLimit = 150;
            var sheet = Game.Game.instance.TableSheets.WorldBossCharacterSheet;
            if (sheet.TryGetValue(bossId, out var boss))
            {
                turnLimit = boss.WaveStats.FirstOrDefault().TurnLimit;
            }

            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Raid);
            var level = States.Instance.CurrentAvatarState.level;
            comboText.comboMax = AttackCountHelper.GetCountMax(level);
            comboText.Close();
            playerStatus.SetData(equipments, costumes, turnLimit);
            progressBar.Clear(bossId);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            progressBar.Show();
        }

        public void UpdateScore(long score)
        {
            progressBar.UpdateScore(score);
        }

        public void OnWaveCompleted()
        {
            progressBar.CompleteWave();
        }

        protected override void OnCompleteOfCloseAnimationInternal()
        {
            base.OnCompleteOfCloseAnimationInternal();
            progressBar.Close();
        }

        public void SetBossProfile(Enemy enemy, int turnLimit)
        {
            bossStatus.SetProfile(enemy);
            bossStatus.Show();
            playerStatus.UpdateTurnLimit(turnLimit);
        }

        public void UpdateStatus(
            long currentHp,
            long maxHp,
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
