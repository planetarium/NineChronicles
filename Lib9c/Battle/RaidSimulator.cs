using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;

namespace Nekoyume.Battle
{
    public class RaidSimulator : Simulator
    {
        public int BossId { get; private set; }
        public int DamageDealt { get; private set; }
        public List<FungibleAssetValue> AssetReward { get; private set; } = new List<FungibleAssetValue>();
        public override IEnumerable<ItemBase> Reward => new List<ItemBase>();
        private readonly List<RaidBoss> _waves;

        private WorldBossBattleRewardSheet _worldBossBattleRewardSheet;
        private RuneWeightSheet _runeWeightSheet;
        private RuneSheet _runeSheet;
        private WorldBossCharacterSheet.Row _currentBossRow;

        public RaidSimulator(
            int bossId,
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            List<RuneState> runeStates,
            RaidSimulatorSheets simulatorSheets,
            CostumeStatSheet costumeStatSheet) : base(random, avatarState, foods, simulatorSheets)
        {
            Player.SetCostumeStat(costumeStatSheet);
            if (runeStates != null)
            {
                Player.SetRune(runeStates, simulatorSheets.RuneOptionSheet, simulatorSheets.SkillSheet);
            }

            BossId = bossId;
            _waves = new List<RaidBoss>();

            if (!simulatorSheets.WorldBossCharacterSheet.TryGetValue(bossId, out _currentBossRow))
                throw new SheetRowNotFoundException(nameof(WorldBossCharacterSheet), bossId);

            if (!simulatorSheets.WorldBossActionPatternSheet.TryGetValue(bossId, out var patternRow))
                throw new SheetRowNotFoundException(nameof(WorldBossActionPatternSheet), bossId);

            _worldBossBattleRewardSheet = simulatorSheets.WorldBossBattleRewardSheet;
            _runeWeightSheet = simulatorSheets.RuneWeightSheet;
            _runeSheet = simulatorSheets.RuneSheet;

            SetEnemies(_currentBossRow, patternRow);
        }

        private void SetEnemies(
            WorldBossCharacterSheet.Row characterRow,
            WorldBossActionPatternSheet.Row patternRow)
        {
            for (var i = 0; i < characterRow.WaveStats.Count; ++i)
            {
                var enemyModel = new RaidBoss(
                    Player,
                    characterRow,
                    patternRow,
                    characterRow.WaveStats[i],
                    true);
                _waves.Add(enemyModel);
            }
        }

        public void SpawnBoss(RaidBoss raidBoss)
        {
            Player.Targets.Add(raidBoss);
            Characters.Enqueue(raidBoss, TurnPriority / raidBoss.SPD);
            raidBoss.InitAI();

            var enemies = new List<Enemy>() { new RaidBoss(raidBoss) };
            var spawnWave = new SpawnWave(null, WaveNumber, WaveTurn, enemies, true);
            Log.Add(spawnWave);
        }


        public BattleLog Simulate()
        {
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 0;

            var turnLimitExceeded = false;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;

                var currentWaveBoss = _waves[i];
                SpawnBoss(currentWaveBoss);

                var waveStatData = currentWaveBoss.RowData.WaveStats
                    .FirstOrDefault(x => x.Wave == WaveNumber);
                while (true)
                {
                    // On turn limit exceeded, player loses.
                    if (TurnNumber > waveStatData.TurnLimit)
                    {
                        turnLimitExceeded = true;
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
                        break;
                    }

                    if (!Characters.TryDequeue(out var character))
                        break;

                    // Boss enrages on EnrageTurn. (EnrageTurn is counted in individual waves.)
                    if (WaveTurn >= waveStatData.EnrageTurn &&
                        !currentWaveBoss.Enraged)
                    {
                        currentWaveBoss.Enrage();
                    }

                    character.Tick();

                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
                        break;
                    }

                    // If targets are all gone
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;
                        break;
                    }

                    foreach (var other in Characters)
                    {
                        var current = Characters.GetPriority(other);
                        var speed = current * 0.6m;
                        Characters.UpdatePriority(other, speed);
                    }

                    Characters.Enqueue(character, TurnPriority / character.SPD);
                }

                // If turn limit exceeded or player died
                if (turnLimitExceeded || Player.IsDead)
                    break;
            }

            foreach (var wave in _waves)
            {
                var leftHp = wave.CurrentHP > 0 ? wave.CurrentHP : 0;
                DamageDealt += wave.HP - leftHp;
            }

            var rank =  WorldBossHelper.CalculateRank(_currentBossRow, DamageDealt);
            AssetReward = RuneHelper.CalculateReward(
                rank,
                BossId,
                _runeWeightSheet,
                _worldBossBattleRewardSheet,
                _runeSheet,
                Random);

            Log.result = Result;
            return Log;
        }
    }
}
