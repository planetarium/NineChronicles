using System;
using System.Collections.Generic;
using Nekoyume.Battle;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    [Serializable]
    public class EnemyPlayer : Player
    {
        public readonly string NameWithHash;
        public EnemyPlayer(AvatarState avatarState, Simulator simulator) : base(avatarState, simulator)
        {
            NameWithHash = avatarState.NameWithHash;
        }

        public EnemyPlayer(
            AvatarState avatarState,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        ) : base(
            avatarState,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet
        )
        {
            NameWithHash = avatarState.NameWithHash;
        }

        public EnemyPlayer(EnemyPlayerDigest enemyPlayerDigest,
            CharacterSheet characterSheet,
            CharacterLevelSheet levelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        ) : base(
            enemyPlayerDigest.Level,
            characterSheet,
            levelSheet,
            equipmentItemSetEffectSheet)
        {
            NameWithHash = enemyPlayerDigest.NameWithHash;
            weapon = null;
            armor = null;
            belt = null;
            necklace = null;
            ring = null;
            monsterMap = new CollectionMap();
            eventMap = new CollectionMap();
            hairIndex = enemyPlayerDigest.HairIndex;
            lensIndex = enemyPlayerDigest.LensIndex;
            earIndex = enemyPlayerDigest.EarIndex;
            tailIndex = enemyPlayerDigest.TailIndex;
            equipments = enemyPlayerDigest.Equipments as List<Equipment>;
            costumes = enemyPlayerDigest.Costumes as List<Costume>;
            characterLevelSheet = levelSheet;
            AttackCountMax = AttackCountHelper.GetCountMax(Level);
            SetEquipmentStat(equipmentItemSetEffectSheet);
        }

        public EnemyPlayer(
            int level,
            CharacterSheet characterSheet,
            CharacterLevelSheet characterLevelSheet,
            EquipmentItemSetEffectSheet equipmentItemSetEffectSheet
        ) : base(
            level,
            characterSheet,
            characterLevelSheet,
            equipmentItemSetEffectSheet
        )
        {
        }

        public EnemyPlayer(AvatarState avatarState, SimulatorSheetsV1 simulatorSheets) : base(
            avatarState, simulatorSheets)
        {
            NameWithHash = avatarState.NameWithHash;
        }

        private EnemyPlayer(EnemyPlayer value) : base(value)
        {
            NameWithHash = value.NameWithHash;
        }

        public EnemyPlayer(ArenaPlayerDigest arenaPlayerDigest, ArenaSimulatorSheetsV1 simulatorSheets)
            : base(arenaPlayerDigest, simulatorSheets)
        {
            NameWithHash = arenaPlayerDigest.NameWithHash;
        }

        public override void Spawn()
        {
            InitAI();
            var spawn = new SpawnEnemyPlayer((CharacterBase)Clone());
            Simulator.Log.Add(spawn);
        }

        [Obsolete("Use Spawn")]
        public override void SpawnV1()
        {
            InitAIV1();
            var spawn = new SpawnEnemyPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        [Obsolete("Use Spawn")]
        public override void SpawnV2()
        {
            InitAIV2();
            var spawn = new SpawnEnemyPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        public override object Clone()
        {
            return new EnemyPlayer(this);
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) Targets[0];
            player.RemoveTarget(this);
        }
    }
}
