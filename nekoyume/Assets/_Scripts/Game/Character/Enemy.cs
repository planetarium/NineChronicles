using BTAI;
using Nekoyume.Data.Table;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : Base
    {
        public int RewardExp = 0;
        public Player Target;

        public void InitAI(Monster data)
        {
            HP = data.Health;
            ATK = data.Attack;
            DEF = data.Defense;
            RewardExp = data.RewardExp;
            _walkSpeed = -0.6f;
            Root = new BTAI.Root();
            Root.OpenBranch(
                BT.If(() => Walkable).OpenBranch(
                    BT.Call(Walk)
                )
            );
        }

        public void OnDamage(int dmg)
        {
            HP -= dmg;
            Debug.Log($"Enemy HP: {HP}");
            if (IsDead())
            {
                OnDead();
            }
        }

        public void OnDead()
        {
            gameObject.SetActive(false);
            Target.OnTargetDead(gameObject);
        }
    }
}
