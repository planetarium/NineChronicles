// #define TEST_LOG

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
            var turn = 1;
#if TEST_LOG
            UnityEngine.Debug.LogWarning($"{nameof(turn)}: {turn} / turn start");
#endif
            WaveTurn = 0;
            while (true)
            {
                if (turn > MaxTurn)
                {
                    Lose = true;
                    Result = BattleLog.Result.TimeOver;
#if TEST_LOG
                        UnityEngine.Debug.LogWarning($"{nameof(turn)}: {turn} / {nameof(Result)}: {Result.ToString()}");
#endif
                    break;
                }
                
                // 캐릭터 큐가 비어 있는 경우 break.
                if (!Characters.TryDequeue(out var character))
                    break;

                character.Tick(out var isTurnEnd);
                if (isTurnEnd)
                {
                    turn++;
#if TEST_LOG
                        UnityEngine.Debug.LogWarning($"{nameof(turn)}: {turn} / {nameof(isTurnEnd)}");
#endif
                }
                
                // 플레이어가 죽은 경우 break;
                if (Player.IsDead)
                {
                    Result = BattleLog.Result.Lose;
                    break;
                }

                // 플레이어의 타겟(적)이 없는 경우 break.
                if (!Player.Targets.Any())
                {
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

            Log.result = Result;
#if TEST_LOG
            var skillType = typeof(Nekoyume.Model.BattleStatus.Skill);
            var skillCount = Log.events.Count(e => e.GetType().IsInheritsFrom(skillType));
            UnityEngine.Debug.LogWarning($"{nameof(turn)}: {turn} / {skillCount} / {nameof(Simulate)} end / {Result.ToString()}");
#endif
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
