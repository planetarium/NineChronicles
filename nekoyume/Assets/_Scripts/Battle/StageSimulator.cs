// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
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
        public int Exp { get; }
        public int TurnLimit { get; }
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

            var stageSheet = TableSheets.StageSheet;
            if (!stageSheet.TryGetValue(StageId, out var stageRow))
                throw new SheetRowNotFoundException(nameof(stageSheet), StageId.ToString());

            var stageWaveSheet = TableSheets.StageWaveSheet;
            if (!stageWaveSheet.TryGetValue(StageId, out var stageWaveRow))
                throw new SheetRowNotFoundException(nameof(stageWaveSheet), StageId.ToString());

            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
            TurnLimit = stageRow.TurnLimit;

            SetWave(stageWaveRow);
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
            Log.worldId = WorldId;
            Log.stageId = StageId;
            Player.Spawn();
            Turn = 1;
#if TEST_LOG
            UnityEngine.Debug.LogWarning($"{nameof(Turn)}: {Turn} / Turn start");
#endif
            for (var i = 0; i < _waves.Count; i++)
            {
                var wave = _waves[i];
                WaveTurn = 0;
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);
                wave.Spawn(this);
                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (Turn > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Player.GetExp((int) (Exp * 0.3m), true);
                            Lose = true;
                            Result = BattleLog.Result.Lose;
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }
#if TEST_LOG
                        UnityEngine.Debug.LogWarning($"{nameof(Turn)}: {Turn} / {nameof(Result)}: {Result.ToString()}");
#endif
                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                        break;

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        Result = i == 0
                            ? BattleLog.Result.Lose
                            : BattleLog.Result.Win;
                        break;
                    }

                    // 플레이어의 타겟(적)이 없는 경우 break.
                    if (!Player.Targets.Any())
                    {
                        var waveEnd = new WaveEnd(null, i);
                        Log.Add(waveEnd);
                        switch (i)
                        {
                            case 0:
                                Player.GetExp(Exp, true);
                                break;
                            case 1:
                            {
                                ItemMap = Player.GetRewards(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            }
                            case 2:
                            {
                                Result = BattleLog.Result.Win;
                                break;
                            }
                        }

                        Result = BattleLog.Result.Win;
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
            var skillType = typeof(Nekoyume.Model.BattleStatus.Skill);
            var skillCount = Log.events.Count(e => e.GetType().IsInheritsFrom(skillType));
            UnityEngine.Debug.LogWarning($"{nameof(Turn)}: {Turn} / {skillCount} / {nameof(Simulate)} end / {Result.ToString()}");
#endif
            return Player;
        }

        private void SetWave(StageWaveSheet.Row stageWaveRow)
        {
            var waves = stageWaveRow.Waves;
            foreach (var wave in waves.Select(SpawnWave))
            {
                _waves.Add(wave);
            }
        }

        private Wave SpawnWave(StageWaveSheet.WaveData waveData)
        {
            var wave = new Wave();
            var monsterTable = TableSheets.CharacterSheet;
            foreach (var monsterData in waveData.Monsters)
            {
                for (var i = 0; i < monsterData.Count; i++)
                {
                    monsterTable.TryGetValue(monsterData.CharacterId, out var row, true);
                    var enemyModel = new Enemy(Player, row, monsterData.Level);

                    wave.Add(enemyModel);
                    wave.IsBoss = waveData.IsBoss;
                }
            }

            wave.Exp = waveData.Number == 2
                ? Exp
                : 0;

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
