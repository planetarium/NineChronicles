// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class EventDungeonBattleSimulator : Simulator, IEnemySkillSheetContainedSimulator
    {
        private readonly int _eventDungeonId;
        private readonly int _eventDungeonStageId;
        private readonly bool _isCleared;
        private readonly int _exp;
        private readonly int _turnLimit;

        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;

        public EnemySkillSheet EnemySkillSheet { get; }

        public override IEnumerable<ItemBase> Reward => _waveRewards;

        public EventDungeonBattleSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int eventDungeonId,
            int eventDungeonStageId,
            EventDungeonBattleSimulatorSheets eventDungeonBattleSimulatorSheets,
            int playCount,
            bool isCleared,
            int exp)
            : base(
                random,
                avatarState,
                foods,
                eventDungeonBattleSimulatorSheets
            )
        {
            _waves = new List<Wave>();

            _eventDungeonId = eventDungeonId;
            _eventDungeonStageId = eventDungeonStageId;
            _isCleared = isCleared;
            _exp = exp;
            EnemySkillSheet = eventDungeonBattleSimulatorSheets.EnemySkillSheet;

            var stageSheet = eventDungeonBattleSimulatorSheets.EventDungeonStageSheet;
            if (!stageSheet.TryGetValue(eventDungeonStageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(nameof(stageSheet), eventDungeonStageId);
            }

            _turnLimit = stageRow.TurnLimit;

            var stageWaveSheet = eventDungeonBattleSimulatorSheets.EventDungeonStageWaveSheet;
            if (!stageWaveSheet.TryGetValue(eventDungeonStageId, out var stageWaveRow))
            {
                throw new SheetRowNotFoundException(nameof(stageWaveSheet), eventDungeonStageId);
            }

            SetWave(stageRow, stageWaveRow);
            var maxCount = Random.Next(
                stageRow.DropItemMin,
                stageRow.DropItemMax + 1);
            _waveRewards = new List<ItemBase>();
            for (var i = 0; i < playCount; i++)
            {
                var rewards = SetRewardV2(
                    SetItemSelector(stageRow, Random),
                    maxCount,
                    Random,
                    MaterialItemSheet
                );

                foreach (var reward in rewards)
                {
                    _waveRewards.Add(reward);
                }
            }
            
            Player.SetCostumeStat(eventDungeonBattleSimulatorSheets.CostumeStatSheet);
        }

        public void Simulate(int playCount)
        {
            Log.worldId = _eventDungeonId;
            Log.stageId = _eventDungeonStageId;
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
                _waves[i].Spawn(this);

                while (true)
                {
                    // NOTE: Break when the turn is over. 
                    if (TurnNumber > _turnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            Player.GetExp((int)(_exp * 0.3m * playCount), true);
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }

                        break;
                    }

                    // NOTE: Break when the character queue is empty.
                    if (!Characters.TryDequeue(out var character))
                    {
                        break;
                    }

                    character.Tick();

                    // NOTE: Break when player is dead.
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            Player.GetExp((int)(_exp * 0.3m * playCount), true);
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }

                        break;
                    }

                    // NOTE: Break when no target is found.
                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        Log.clearedWaveNumber = WaveNumber;

                        switch (WaveNumber)
                        {
                            case 1:
                            {
                                Player.GetExp(_exp * playCount, true);
                                break;
                            }
                            case 2:
                            {
                                Player.GetRewards(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                            {
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!_isCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }

                                break;
                            }
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

                // NOTE: Break when the turn is over or the player is dead.
                if (TurnNumber > _turnLimit ||
                    Player.IsDead)
                {
                    break;
                }
            }

            Log.result = Result;
        }

        private void SetWave(
            StageSheet.Row stageRow,
            StageWaveSheet.Row stageWaveRow)
        {
            var enemyStatModifiers = stageRow.EnemyOptionalStatModifiers;
            var waves = stageWaveRow.Waves;
            foreach (var wave in waves.Select(e => SpawnWave(e, enemyStatModifiers)))
            {
                _waves.Add(wave);
            }
        }

        private Wave SpawnWave(
            StageWaveSheet.WaveData waveData,
            IReadOnlyList<StatModifier> optionalStatModifiers)
        {
            var wave = new Wave();
            foreach (var monsterData in waveData.Monsters)
            {
                for (var i = 0; i < monsterData.Count; i++)
                {
                    CharacterSheet.TryGetValue(
                        monsterData.CharacterId,
                        out var row,
                        true);
                    var enemyModel = new Enemy(
                        Player,
                        row,
                        monsterData.Level,
                        optionalStatModifiers);

                    wave.Add(enemyModel);
                    wave.HasBoss = waveData.HasBoss;
                }
            }

            return wave;
        }

        private static WeightedSelector<StageSheet.RewardData> SetItemSelector(
            StageSheet.Row stageRow,
            IRandom random)
        {
            var itemSelector = new WeightedSelector<StageSheet.RewardData>(random);
            foreach (var r in stageRow.Rewards)
            {
                itemSelector.Add(r, r.Ratio);
            }

            return itemSelector;
        }
    }
}
