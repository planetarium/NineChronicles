using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.UI;

namespace Nekoyume.Model
{
    [Serializable]
    public class BattleResult : EventBase
    {
        public Result result;
        public enum Result
        {
            Win,
            Lose
        }

        public override void Execute(Game.Character.Player player, IEnumerable<Enemy> enemies)
        {
            var blind = Widget.Find<Blind>();
            Task.Run(() => blind.FadeIn(1.0f, result.ToString()));
            Event.OnRoomEnter.Invoke();
        }
    }
}
