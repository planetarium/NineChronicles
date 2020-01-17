using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public class RankingSimulator : Simulator
    {
        private readonly EnemyPlayer _enemyPlayer;

        public RankingSimulator(
            IRandom random,
            AvatarState avatarState,
            AvatarState enemyAvatarState,
            List<Consumable> foods,
            TableSheets tableSheets) : base(random, avatarState, foods, tableSheets)
        {
            _enemyPlayer = new EnemyPlayer(enemyAvatarState, this);
            _enemyPlayer.Stats.EqualizeCurrentHPWithHP();
        }

        public override Player Simulate()
        {
            Spawn();
            Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            Characters.Enqueue(Player, TurnPriority / Player.SPD);
            Characters.Enqueue(_enemyPlayer, TurnPriority / _enemyPlayer.SPD);
            var turn = 0;
            var spdList = Characters.ToList();
            var spdList2 = new List<CharacterBase>();
            var waveTurn = 0;
            while (true)
            {
                turn++;
                if (turn >= MaxTurn)
                {
                    Result = BattleLog.Result.TimeOver;
                    Lose = true;
                    break;
                }

                if (!spdList.Any())
                {
                    spdList.AddRange(spdList2);
                }

                if (Characters.TryDequeue(out var character))
                {
                    spdList.Remove(character);
                    character.Tick();
                    spdList2.Add(character);
                }
                else
                {
                    break;
                }

                if (!spdList.Any())
                {
                    var e = new WaveTurnEnd(character, waveTurn);
                    Log.Add(e);
                    waveTurn++;
                }

                if (!Player.Targets.Any())
                {
                    Result = BattleLog.Result.Win;
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

                if (Lose)
                {
                    break;
                }
            }

            Log.result = Result;
            return Player;
        }

        private void Spawn()
        {
            Player.Spawn();
            _enemyPlayer.Spawn();
            Player.Targets.Add(_enemyPlayer);
            _enemyPlayer.Targets.Add(Player);
        }
    }
}
