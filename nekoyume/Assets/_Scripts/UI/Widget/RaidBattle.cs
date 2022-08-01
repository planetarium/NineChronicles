using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class RaidBattle : Widget
    {
        [SerializeField]
        private BossStatus bossStatus;

        [SerializeField]
        private ComboText comboText;

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnGetItem.AddListener(_ =>
            {
                var headerMenu = Find<HeaderMenuStatic>();
                if (!headerMenu)
                {
                    throw new WidgetNotFoundException<HeaderMenuStatic>();
                }

                var target = headerMenu.GetToggle(HeaderMenuStatic.ToggleType.AvatarInfo);
                VFXController.instance.CreateAndChase<DropItemInventoryVFX>(target, Vector3.zero);
            });
            CloseWidget = null;
        }

        public void Show(
            CharacterBase player,
            bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().Show(HeaderMenuStatic.AssetVisibleState.WorldBoss);
            comboText.comboMax = AttackCountHelper.GetCountMax(player.Level);
            comboText.Close();
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            bossStatus.Close(ignoreCloseAnimation);
            Find<HeaderMenuStatic>().Close();
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
