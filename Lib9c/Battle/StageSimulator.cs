// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Model.Buff;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class StageSimulator : Simulator, IStageSimulator
    {
        private readonly List<Wave> _waves;
        private readonly List<ItemBase> _waveRewards;
        private readonly List<Model.Skill.Skill> _skillsOnWaveStart;

        public CollectionMap ItemMap { get; private set; } = new CollectionMap();
        public EnemySkillSheet EnemySkillSheet { get; }

        private int WorldId { get; }
        public int StageId { get; }
        private bool IsCleared { get; }
        private int Exp { get; }
        private int TurnLimit { get; }
        public override IEnumerable<ItemBase> Reward => _waveRewards;

        public StageSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            List<RuneState> runeStates,
            List<Model.Skill.Skill> skillsOnWaveStart,
            int worldId,
            int stageId,
            StageSheet.Row stageRow,
            StageWaveSheet.Row stageWaveRow,
            bool isCleared,
            int exp,
            SimulatorSheets simulatorSheets,
            EnemySkillSheet enemySkillSheet,
            CostumeStatSheet costumeStatSheet,
            List<ItemBase> waveRewards)
            : base(
                random,
                avatarState,
                foods,
                simulatorSheets)
        {
            Player.SetCostumeStat(costumeStatSheet);
            if (runeStates != null)
            {
                Player.SetRune(runeStates, simulatorSheets.RuneOptionSheet, simulatorSheets.SkillSheet);
            }

            _waves = new List<Wave>();
            _waveRewards = waveRewards;
            WorldId = worldId;
            StageId = stageId;
            IsCleared = isCleared;
            Exp = exp;
            EnemySkillSheet = enemySkillSheet;
            TurnLimit = stageRow.TurnLimit;
            _skillsOnWaveStart = skillsOnWaveStart;

            SetWave(stageRow, stageWaveRow);
        }

        public static List<ItemBase> GetWaveRewards(
            IRandom random,
            StageSheet.Row stageRow,
            MaterialItemSheet materialItemSheet,
            int playCount = 1)
        {
            var maxCountForItemDrop = random.Next(
                stageRow.DropItemMin,
                stageRow.DropItemMax + 1);
            var waveRewards = new List<ItemBase>();
            for (var i = 0; i < playCount; i++)
            {
                var itemSelector = StageSimulatorV1.SetItemSelector(stageRow, random);
                var rewards = SetRewardV2(
                    itemSelector,
                    maxCountForItemDrop,
                    random,
                    materialItemSheet
                );

                waveRewards.AddRange(rewards);
            }

            return waveRewards;
        }

        public Player Simulate()
        {
            Log.worldId = WorldId;
            Log.stageId = StageId;
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

                foreach (var skill in _skillsOnWaveStart)
                {
                    var buffs = BuffFactory.GetBuffs(
                        Player.ATK,
                        skill,
                        SkillBuffSheet,
                        StatBuffSheet,
                        SkillActionBuffSheet,
                        ActionBuffSheet
                    );

                    var usedSkill = skill.Use(Player, 0, buffs);
                    Log.Add(usedSkill);
                }

                while (true)
                {
                    // 제한 턴을 넘어서는 경우 break.
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (Exp > 0)
                            {
                                Player.GetExp((int)(Exp * 0.3m), true);
                            }
                        }
                        else
                        {
                            Result = BattleLog.Result.TimeOver;
                        }

                        break;
                    }

                    // 캐릭터 큐가 비어 있는 경우 break.
                    if (!Characters.TryDequeue(out var character))
                    {
                        break;
                    }

                    character.Tick();

                    // 플레이어가 죽은 경우 break;
                    if (Player.IsDead)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (Exp > 0)
                            {
                                Player.GetExp((int)(Exp * 0.3m), true);
                            }
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
                            {
                                if (Exp > 0)
                                {
                                    Player.GetExp(Exp, true);
                                }

                                break;
                            }
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
                            {
                                if (WaveNumber == _waves.Count)
                                {
                                    if (!IsCleared)
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

                // 제한 턴을 넘거나 플레이어가 죽은 경우 break;
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }

        private void SetWave(StageSheet.Row stageRow, StageWaveSheet.Row stageWaveRow)
        {
            var enemyStatModifiers = stageRow.EnemyOptionalStatModifiers;
            var waves = stageWaveRow.Waves;
            foreach (var wave in waves
                         .Select(e => SpawnWave(e, enemyStatModifiers)))
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
                    var enemyModel = new Enemy(Player, row, monsterData.Level,
                        optionalStatModifiers);

                    wave.Add(enemyModel);
                    wave.HasBoss = waveData.HasBoss;
                }
            }

            return wave;
        }
    }
}
