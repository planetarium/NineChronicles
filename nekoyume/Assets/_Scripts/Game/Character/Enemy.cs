using BTAI;
using DG.Tweening;
using Nekoyume.Data.Table;
using UnityEngine;


namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int RewardExp = 0;

        public void InitAI()
        {
            _walkSpeed = -1.0f;
            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);

            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Selector().OpenBranch(
                            BT.If(HasTargetInRange).OpenBranch(
                                BT.Call(Attack)
                            ),
                            BT.Call(Walk)
                        )
                    ),
                    BT.Sequence().OpenBranch(
                        BT.Call(Die),
                        BT.Terminate()
                    )
                )
            );

            _skills.Clear();
            // TODO: select skill
            var attack = this.GetOrAddComponent<Skill.MonsterAttack>();
            if (attack.Init("monster_attack"))
            {
                _skills.Add(attack);
            }
        }

        public void InitStats(Data.Table.Monster statsData, int power)
        {
            HP = statsData.Health;
            ATK = statsData.Attack;
            DEF = statsData.Defense;
            WeightType = statsData.WeightType;
            RewardExp = statsData.RewardExp;

            Power = power;

            _hpMax = HP;
        }

        public override void OnDamage(AttackType attackType, int dmg)
        {
            base.OnDamage(attackType, dmg);

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                DG.Tweening.Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.red, 0.1f));
                colorseq.Append(mat.DOColor(Color.white, 0.1f));
            }
        }

        override protected void OnDead()
        {
            base.OnDead();
            Event.OnEnemyDead.Invoke(this);
        }
    }
}
