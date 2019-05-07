using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using Nekoyume.UI;
using UnityEngine.Events;

namespace Nekoyume.Game
{
    public static class Event
    {
        public static readonly UnityEvent OnNestEnter = new UnityEvent();
        public static readonly UnityEvent OnRoomEnter = new UnityEvent();
        public static readonly UnityEvent OnStageStart = new UnityEvent();
        public static readonly UnityEvent OnPlayerDead = new UnityEvent();

        public class EnemyDead : UnityEvent<Enemy>
        {
        }
        public static readonly EnemyDead OnEnemyDead = new EnemyDead();
        public static readonly UnityEvent OnStageClear = new UnityEvent();

        public static readonly UnityEvent OnUpdateStatus = new UnityEvent();

        public class GetItem : UnityEvent<Item.DropItem>
        {
        }
        public static readonly  GetItem OnGetItem = new GetItem();

        public static readonly UnityEvent OnUseSkill = new UnityEvent();

        public class LoginDetail : UnityEvent<int>
        {
        }
        public static readonly LoginDetail OnLoginDetail = new LoginDetail();

        public class SlotClick : UnityEvent<InventorySlot, bool>
        {
        }
        public static readonly  SlotClick OnSlotClick = new SlotClick();

        public class AttackEnd : UnityEvent<CharacterBase>
        {
        }
        public static readonly AttackEnd OnAttackEnd = new AttackEnd();
    }
}
