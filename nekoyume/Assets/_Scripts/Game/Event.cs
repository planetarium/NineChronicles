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
        public static readonly UnityEvent OnNestEnter = new();
        public static readonly UnityEvent<bool> OnLobbyEnter = new();
        public static readonly UnityEvent OnPlayerDead = new();

        public static readonly UnityEvent<StageMonster> OnEnemyDeadStart = new();

        public static readonly Subject<Player> OnUpdatePlayerEquip = new();
        public static readonly Subject<Player> OnUpdatePlayerStatus = new();

        public static readonly UnityEvent<DropItem> OnGetItem = new();

        public static readonly UnityEvent<int> OnLoginDetail = new();

        public static readonly UnityEvent<long> OnWaveStart = new();

        public static readonly UnityEvent<int> OnPlayerTurnEnd = new();

        public static readonly UnityEvent OnUpdateAddresses = new();

        public static readonly UnityEvent OnUpdateRuneState = new();
    }
}
