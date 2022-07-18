// #define TEST_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Event;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class EventDungeonBattleSimulator : StageSimulator
    {
        public EventDungeonBattleSimulator(
            IRandom random,
            AvatarState avatarState,
            List<Guid> foods,
            int eventDungeonId,
            int eventDungeonStageId,
            EventDungeonBattleSimulatorSheets eventDungeonBattleSimulatorSheets,
            int constructorVersion,
            bool isCleared,
            int exp,
            int playCount
        )
            : base(
                random,
                avatarState,
                foods,
                eventDungeonId,
                eventDungeonStageId,
                eventDungeonBattleSimulatorSheets,
                eventDungeonBattleSimulatorSheets.CostumeStatSheet,
                constructorVersion,
                playCount
            )
        {
            IsCleared = isCleared;
            Exp = exp;
        }

        public override Player Simulate(int playCount)
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
                        skill,
                        SkillBuffSheet,
                        BuffSheet
                    );

                    var usedSkill = skill.Use(Player, 0, buffs);
                    Log.Add(usedSkill);
                }

                while (true)
                {
                    // NOTE: Break when the turn is over. 
                    if (TurnNumber > TurnLimit)
                    {
                        if (i == 0)
                        {
                            Result = BattleLog.Result.Lose;
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int)(Exp * 0.3m * playCount), true);
                            }
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
                            if (StageId < GameConfig.MimisbrunnrStartStageId)
                            {
                                Player.GetExp((int)(Exp * 0.3m * playCount), true);
                            }
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
                                Player.GetExp(Exp * playCount, true);
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

                // NOTE: Break when the turn is over or the player is dead.
                if (TurnNumber > TurnLimit ||
                    Player.IsDead)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }
    }
}
