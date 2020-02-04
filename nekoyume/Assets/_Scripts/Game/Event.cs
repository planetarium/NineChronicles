using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.Model.BattleStatus;
using Nekoyume.UI;
using UniRx;
using UnityEngine.Events;
using Enemy = Nekoyume.Game.Character.Enemy;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.Game
{
    public static class Event
    {
        public static readonly UnityEvent OnNestEnter = new UnityEvent();
        public static readonly UnityEvent OnRoomEnter = new UnityEvent();
        public static readonly UnityEvent OnPlayerDead = new UnityEvent();

        public class EnemyDead : UnityEvent<Enemy>
        {
        }
        public static readonly EnemyDead OnEnemyDead = new EnemyDead();
        public static readonly EnemyDead OnEnemyLastHit = new EnemyDead();
        public static readonly UnityEvent OnStageClear = new UnityEvent();

        public static readonly Subject<Player> OnUpdatePlayerStatus = new Subject<Player>();

        public class GetItem : UnityEvent<DropItem>
        {
        }
        public static readonly GetItem OnGetItem = new GetItem();

        public static readonly UnityEvent OnUseSkill = new UnityEvent();

        public class LoginDetail : UnityEvent<int>
        {
        }
        public static readonly LoginDetail OnLoginDetail = new LoginDetail();

        public class SlotClick : UnityEvent<InventorySlot, bool>
        {
        }
        public static readonly  SlotClick OnSlotClick = new SlotClick();

        public class StageStart : UnityEvent<BattleLog>
        {
        }
        public static readonly StageStart OnStageStart = new StageStart();
        public static readonly StageStart OnRankingBattleStart = new StageStart();

        public class TipChanged : UnityEvent<long>
        {
        }
        public static readonly TipChanged OnTipChanged = new TipChanged();

    }
}
