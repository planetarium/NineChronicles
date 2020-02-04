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
using UnityEngine;

namespace Nekoyume.Battle
{
    public class StageSimulator : Simulator
    {
        private readonly int _worldId;
        public readonly int StageId;
        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;
        public CollectionMap ItemMap = new CollectionMap();
        
        public IEnumerable<ItemBase> Rewards => _waveRewards;
        public int Exp { get; }

        public StageSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Consumable> foods,
            int worldId,
            int stageId,
            TableSheets tableSheets) : base(random, avatarState, foods, tableSheets)
        {
            _worldId = worldId;
            StageId = stageId;
            _waves = new List<Wave>();
            _waveRewards = new List<ItemBase>();
            SetWave();
            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
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
            if (!ReferenceEquals(skill, null))
                Player.OverrideSkill(skill);
            
            Exp = StageRewardExpHelper.GetExp(avatarState.level, stageId);
        }

        public override Player Simulate()
        {
            Log.worldId = _worldId;
            Log.stageId = StageId;
            Player.Spawn();
            var turn = 0;
            for (var i = 0; i < _waves.Count; i++)
            {
                var wave = _waves[i];
                WaveTurn = 0;
                Characters = new SimplePriorityQueue<CharacterBase, decimal>();
                Characters.Enqueue(Player, TurnPriority / Player.SPD);
                wave.Spawn(this);
                while (true)
                {
                    turn++;
                    if (turn > MaxTurn)
                    {
                        Lose = i == 0;
                        if (Lose)
                        {
                            Player.GetExp((int) (Exp * 0.3m), true);
                            Result = BattleLog.Result.Lose;
                            break;
                        }
                        
                        // todo: 타임오버 대신 부분 승리 처리 필요.
                        Result = BattleLog.Result.TimeOver;
                        break;
                    }

                    if (Characters.TryDequeue(out var character))
                    {
                        character.Tick();
                    }
                    else
                    {
                        break;
                    }

                    if (!Player.Targets.Any())
                    {
                        Result = BattleLog.Result.Win;
                        
                        switch (i)
                        {
                            case 0:
                                Player.GetExp(Exp, true);
                                break;
                            case 1:
                                ItemMap = Player.GetRewards(_waveRewards);
                                var dropBox = new DropBox(null, _waveRewards);
                                Log.Add(dropBox);
                                var getReward = new GetReward(null, _waveRewards);
                                Log.Add(getReward);
                                break;
                            case 2:
                                // todo: 첫 3별 클리어 보상 적용.
                                break;
                        }

                        break;
                    }
                    
                    if (Lose)
                    {
                        Result = BattleLog.Result.Lose;
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

                if (Lose)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }

        private void SetWave()
        {
            var stageWaveSheet = TableSheets.StageWaveSheet;
            if (!stageWaveSheet.TryGetValue(StageId, out var stageWaveRow))
                throw new SheetRowNotFoundException(nameof(stageWaveSheet), StageId.ToString());

            var waves = stageWaveRow.Waves;
            foreach (var wave in waves.Select(SpawnWave))
            {
                _waves.Add(wave);
            }
            
            GetReward(stageWaveRow.StageId);
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

        private void GetReward(int id)
        {
            var itemSelector = new WeightedSelector<int>(Random);
            if (!TableSheets.StageSheet.TryGetValue(id, out var stageRow))
                return;
            
            var rewards = stageRow.Rewards.Where(r => r.Ratio > 0m);
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
                            _waveRewards.Add(item);
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
