using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Battle
{
    public class RaidSimulator : Simulator
    {
        public readonly EnemySkillSheet EnemySkillSheet;

        public override IEnumerable<ItemBase> Reward => throw new System.NotImplementedException();
        private const int TurnLimit = 150;
        private int _bossId;
        private readonly List<RaidBoss> _waves;

        public RaidSimulator(
            int bossId,
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            RaidSimulatorSheets simulatorSheets) : base(random, avatarState, foods, simulatorSheets)
        {
            _bossId = bossId;
            _waves = new List<RaidBoss>();
            EnemySkillSheet = simulatorSheets.EnemySkillSheet;

            if (!simulatorSheets.CharacterSheet.TryGetValue(bossId, out var characterRow))
                throw new SheetRowNotFoundException(nameof(CharacterSheet), bossId);

            if (!simulatorSheets.WorldBossSheet.TryGetValue(bossId, out var bossRow))
                throw new SheetRowNotFoundException(nameof(WorldBossSheet), bossId);

            SetEnemies(characterRow, bossRow);
        }

        private void SetEnemies(
            CharacterSheet.Row characterRow,
            WorldBossSheet.Row worldBossRow)
        {
            for (var i = 0; i < worldBossRow.WaveStats.Count; ++i)
            {
                var stat = new CharacterStats(
                    characterRow,
                    worldBossRow.WaveStats[i]);
                var enemyModel = new RaidBoss(
                    Player,
                    characterRow,
                    stat);
                _waves.Add(enemyModel);
            }
        }

        public void SpawnBoss(RaidBoss raidBoss)
        {
            Player.Targets.Add(raidBoss);
            Characters.Enqueue(raidBoss, TurnPriority / raidBoss.SPD);
            raidBoss.InitAI();

            var enemies = new List<Enemy>() { raidBoss };
            var spawnWave = new SpawnWave(null, WaveNumber, WaveTurn, enemies, true);
            Log.Add(spawnWave);
        }


        public Player Simulate()
        {
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);

                WaveNumber = i + 1;
                WaveTurn = 1;
                SpawnBoss(_waves[i]);
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
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

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
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

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                                break;
                            case 2:
                                break;
                            case 3:
                                break;
                            case 4:
                                break;
                            case 5:
                                break;
                            default:
                                break;
                        }

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

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                    break;
            }

            Log.result = Result;
            return Player;
        }
    }
}
