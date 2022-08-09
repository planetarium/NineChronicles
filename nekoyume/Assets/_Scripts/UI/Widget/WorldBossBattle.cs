using System.Collections.Generic;
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

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = null;
        }

        public void Show(
            Game.Character.Player player,
            bool ignoreShowAnimation = false)
        {
            comboText.comboMax = AttackCountHelper.GetCountMax(player.Level);
            comboText.Close();
            playerStatus.SetData(player);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            bossStatus.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }

        public void SetBossProfile(Enemy enemy)
        {
            bossStatus.SetProfile(enemy);
            bossStatus.Show();
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
