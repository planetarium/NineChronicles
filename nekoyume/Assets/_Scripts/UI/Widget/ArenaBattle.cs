using System.Collections.Generic;
using Libplanet.Crypto;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ArenaBattle : Widget
    {
        [SerializeField]
        private ArenaStatus myStatus;

        [SerializeField]
        private ArenaStatus enemyStatus;

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
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest,
            Address myAvatarAddress,
            Address enemyAvatarAddress,
            TableSheets tableSheets,
            bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().Close(true);
            Find<Status>().Close(true);
            Find<EventBanner>().Close(true);
            SetStatus(myDigest, myStatus, myAvatarAddress, tableSheets, true);
            SetStatus(enemyDigest, enemyStatus, enemyAvatarAddress, tableSheets, true);

            comboText.comboMax = AttackCountHelper.GetCountMax(myDigest.Level);
            comboText.Close();
            base.Show(ignoreShowAnimation);
        }

        public void ShowStatus(bool isEnemy)
        {
            if (isEnemy)
            {
                enemyStatus.Show();
            }
            else
            {
                myStatus.Show();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            myStatus.Close(ignoreCloseAnimation);
            enemyStatus.Close(ignoreCloseAnimation);
            Find<HeaderMenuStatic>().Close();
            base.Close(ignoreCloseAnimation);
        }

        public void UpdateStatus(
            bool isEnemy,
            long currentHp,
            long maxHp,
            Dictionary<int, Nekoyume.Model.Buff.Buff> buffs,
            TableSheets tableSheets,
            bool vfx)
        {
            if (isEnemy)
            {
                enemyStatus.SetHp(currentHp, maxHp);
                enemyStatus.SetBuff(tableSheets, vfx, buffs);
            }
            else
            {
                myStatus.SetHp(currentHp, maxHp);
                myStatus.SetBuff(tableSheets, vfx, buffs);
            }
        }

        public void ShowComboText(bool attacked)
        {
            comboText.StopAllCoroutines();
            comboText.Show(attacked);
        }

        private void SetStatus(ArenaPlayerDigest digest, ArenaStatus status, Address address, TableSheets tableSheets, bool vfx)
        {
            var portraitId = Util.GetPortraitId(digest.Equipments, digest.Costumes);
            status.Set(portraitId, digest.NameWithHash, digest.Level, address, tableSheets, vfx);
            status.gameObject.SetActive(false);
        }
    }
}
