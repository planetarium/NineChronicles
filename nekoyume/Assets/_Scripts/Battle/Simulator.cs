using System.Collections.Generic;
using Libplanet.Action;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public abstract class Simulator
    {
        public readonly IRandom Random;
        public readonly BattleLog Log;
        public bool Lose = false;
        public readonly Player Player;
        public BattleLog.Result Result { get; protected set; }
        public SimplePriorityQueue<CharacterBase, decimal> Characters;
        public const decimal TurnPriority = 100m;
        public readonly TableSheets TableSheets;
        protected const int MaxTurn = 3000;
        public int WaveTurn;

        protected Simulator(
            IRandom random,
            AvatarState avatarState,
            List<Consumable> foods,
            TableSheets tableSheets)
        {
            Random = random;
            TableSheets = tableSheets;
            Log = new BattleLog();
            Player = new Player(avatarState, this);
            Player.Use(foods);
            Player.Stats.EqualizeCurrentHPWithHP();
        }

        protected Simulator(IRandom random, AvatarState avatarState, List<Consumable> foods)
        {
            Random = random;
            TableSheets = Game.Game.instance.TableSheets;
            Log = new BattleLog();
            Player = new Player(avatarState, this);
            Player.Use(foods);
            Player.Stats.EqualizeCurrentHPWithHP();
        }

        public abstract Player Simulate();
    }
}
