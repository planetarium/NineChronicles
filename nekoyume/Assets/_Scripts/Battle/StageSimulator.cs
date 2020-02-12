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
    public class StageSimulator : Simulator
    {
        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;
        public CollectionMap ItemMap = new CollectionMap();

        private int WorldId { get; }
        public int StageId { get; }
        private bool HasCleared { get; }
        private int Exp { get; }
        private int TurnLimit { get; }
        public IEnumerable<ItemBase> Rewards => _waveRewards;

        public StageSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Consumable> foods,
            int worldId,
            int stageId,
            TableSheets tableSheets) : base(random, avatarState, foods, tableSheets)
        {
            _waves = new List<Wave>();
            _waveRewards = new List<ItemBase>();

            WorldId = worldId;
            StageId = stageId;
            HasCleared = avatarState.worldInformation.HasStageCleared(WorldId, StageId);

            var stageSheet = TableSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId.ToString());

            var stageWaveSheet = TableSheets.StageWaveSheet;
            if (!stageWaveSheet.TryGetValue(StageId, out var stageWaveRow))
                throw new SheetRowNotFoundException(nameof(stageWaveSheet), StageId.ToString());

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            SetWave(stageRow, stageWaveRow);
            SetReward(stageRow);
        }

        public StageSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Consumable> foods,
            int worldId,
            int stageId,
            TableSheets tableSheets,
            Model.Skill.Skill skill)
            : this(random, avatarState, foods, worldId, stageId, tableSheets)
        {
            var stageSheet = TableSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId.ToString());

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            if (!ReferenceEquals(skill, null))
            {
                Player.OverrideSkill(skill);
            }
        }

        public override Player Simulate()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Log.waveCount = _waves.Count;
            Log.clearedWaveNumber = 0;
            Log.newlyCleared = false;
            Player.Spawn();
            TurnNumber = 1;
            for (var i = 0; i < _waves.Count; i++)
            {
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);
                
                WaveNumber = i + 1;
                WaveTurn = 1;
                _waves[i].Spawn(this);
#if TEST_LOG
                sb.Clear();
                sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                sb.Append(" / Wave Start");
                UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Player.GetExp((int) (Exp * 0.3m), true);
                            Result = BattleLog.Result.Lose;
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
#if TEST_LOG
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append($" / {nameof(TurnLimit)}: {TurnLimit}");
                        sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                        UnityEngine.Debug.LogWarning(sb.ToString());
#endif
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;
#if TEST_LOG
                    var turnBefore = TurnNumber;
#endif
                    character.Tick();
#if TEST_LOG
                    var turnAfter = TurnNumber;
                    if (turnBefore != turnAfter)
                    {
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append(" / Turn End");
                        UnityEngine.Debug.LogWarning(sb.ToString());   
                    }
#endif

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            Player.GetExp((int) (Exp * 0.3m), true);
                        }
                        else
                        {
                            Result = BattleLog.Result.Win;
                        }
#if TEST_LOG
                        sb.Clear();
                        sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                        sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                        sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                        sb.Append($" / {nameof(Player)} Dead");
                        sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                        UnityEngine.Debug.LogWarning(sb.ToString());
#endif
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
                                Player.GetExp(Exp, true);
                                break;
                            case 2:
                            {
                                ItemMap = Player.GetRewards(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            default:
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!HasCleared)
                                    {
                                        Log.newlyCleared = true;
                                    }
                                }
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

                // 플레이어가 죽은 경우 break;
                if (Player.IsDead)
                    break;
            }

            Log.result = Result;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(Simulate)} End");
            sb.Append($" / {nameof(Result)}: {Result.ToString()}");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            return Player;
        }

        private void SetWave(StageSheet.Row stageRow, StageWaveSheet.Row stageWaveRow)
        {
            var enemyStatModifiers = stageRow.EnemyOptionalStatModifiers;
            var waves = stageWaveRow.Waves;
            foreach (var wave in waves.Select(e => SpawnWave(e, enemyStatModifiers)))
            {
                _waves.Add(wave);
            }
        }

        private Wave SpawnWave(StageWaveSheet.WaveData waveData, IReadOnlyList<StatModifier> optionalStatModifiers)
        {
            var wave = new Wave();
            var monsterTable = TableSheets.CharacterSheet;
            foreach (var monsterData in waveData.Monsters)
            {
                for (var i = 0; i < monsterData.Count; i++)
                {
                    monsterTable.TryGetValue(monsterData.CharacterId, out var row, true);
                    var enemyModel = new Enemy(Player, row, monsterData.Level, optionalStatModifiers);

                    wave.Add(enemyModel);
                    wave.HasBoss = waveData.HasBoss;
                }
            }

            return wave;
        }

        private void SetReward(StageSheet.Row stageRow)
        {
            var itemSelector = new WeightedSelector<int>(Random);
            var rewards = stageRow.Rewards.Where(r => r.Ratio > 0m).OrderBy(r => r.Ratio);
            foreach (var r in rewards)
            {
                itemSelector.Add(r.ItemId, r.Ratio);
                try
                {
                    var itemId = itemSelector.Pop();
                    if (TableSheets.MaterialItemSheet.TryGetValue(itemId, out var itemData))
                    {
                        var count = Random.Next(r.Min, r.Max + 1);
                        for (var i = 0; i < count; i++)
                        {
                            var guid = Random.GenerateRandomGuid();
                            var item = ItemFactory.Create(itemData, guid);
                            if (_waveRewards.Count < 4)
                            {
                                _waveRewards.Add(item);
                            }
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }
    }
}
