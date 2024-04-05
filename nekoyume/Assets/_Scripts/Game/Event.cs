using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using UniRx;
using UnityEngine.Events;
using Player = Nekoyume.Game.Character.Player;

namespace Nekoyume.Game
{
    public static class Event
    {
        public static readonly UnityEvent OnNestEnter = new UnityEvent();
        public static readonly UnityEvent<bool> OnRoomEnter = new UnityEvent<bool>();
        public static readonly UnityEvent OnPlayerDead = new UnityEvent();

        public static readonly UnityEvent<StageMonster> OnEnemyDeadStart = new UnityEvent<StageMonster>();

        public static readonly Subject<Player> OnUpdatePlayerEquip = new Subject<Player>();
        public static readonly Subject<Player> OnUpdatePlayerStatus = new Subject<Player>();

        public static readonly UnityEvent<DropItem> OnGetItem = new UnityEvent<DropItem>();

        public static readonly UnityEvent<int> OnLoginDetail = new UnityEvent<int>();

        public static readonly UnityEvent<long> OnWaveStart = new UnityEvent<long>();

        public static readonly UnityEvent<int> OnPlayerTurnEnd = new UnityEvent<int>();

        public static readonly UnityEvent OnUpdateAddresses = new UnityEvent();

        public static readonly UnityEvent OnUpdateRuneState = new();
    }
}
