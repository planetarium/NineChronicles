using System.Linq;
using BTAI;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Enemy : Character
    {
        public new Monster Stats;

        protected override void Walk()
        {
            Vector2 position = transform.position;
            position.x -= Time.deltaTime * 40 / 160;
            transform.position = position;

        }

        public void _Load(string code)
        {
            SetPosition(3);
            RenderLoad(code);
            var tables = this.GetRootComponent<Tables>();
            var statsTable = tables.Monster;
            Stats = statsTable[code];
            Root = new Root();
            Root.OpenBranch(
                BT.Call(Walk)
            );
        }
    }
}