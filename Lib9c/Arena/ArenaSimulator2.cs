using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Arena
{
    /// <summary>
    /// This class is only for previewnet.
    /// </summary>
    public class ArenaSimulator2 : Simulator
    {
        private readonly EnemyPlayer _enemyPlayer;

        public ArenaSimulator2(IRandom random,
            ArenaPlayerDigest myDigest,
            ArenaPlayerDigest enemyDigest,
            ArenaSimulatorSheetsV1 simulatorSheets
        ) : base(
            random,
            new Player(myDigest, simulatorSheets),
            new List<Guid>(),
            simulatorSheets
        )
        {
            _enemyPlayer = new EnemyPlayer(enemyDigest, simulatorSheets)
            {
                Simulator = this
            };
        }

        public Player Simulate()
        {
#if TEST_LOG
            var sb = new System.Text.StringBuilder();
#endif
            Spawn();
            Characters = new SimplePriorityQueue<CharacterBase, decimal>();
            Characters.Enqueue(Player, TurnPriority / Player.SPD);
            Characters.Enqueue(_enemyPlayer, TurnPriority / _enemyPlayer.SPD);
            TurnNumber = 1;
            WaveNumber = 1;
            WaveTurn = 1;
#if TEST_LOG
            sb.Clear();
            sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
            sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
            sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
            sb.Append($" / {nameof(WaveNumber)} Start");
            UnityEngine.Debug.LogWarning(sb.ToString());
#endif
            while (true)
            {
                if (TurnNumber > MaxTurn)
                {
                    Result = BattleLog.Result.TimeOver;
#if TEST_LOG
                    sb.Clear();
                    sb.Append($"{nameof(TurnNumber)}: {TurnNumber}");
                    sb.Append($" / {nameof(WaveNumber)}: {WaveNumber}");
                    sb.Append($" / {nameof(WaveTurn)}: {WaveTurn}");
                    sb.Append($" / {nameof(MaxTurn)}: {MaxTurn}");
                    sb.Append($" / {nameof(Result)}: {Result.ToString()}");
                    UnityEngine.Debug.LogWarning(sb.ToString());
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
                    Result = BattleLog.Result.Lose;
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

        private void Spawn()
        {
            Player.Spawn();
            _enemyPlayer.Spawn();
            Player.Targets.Add(_enemyPlayer);
            _enemyPlayer.Targets.Add(Player);
        }


        /// <summary>
        /// do not use it
        /// </summary>
        public override IEnumerable<ItemBase> Reward { get; }
    }
}
