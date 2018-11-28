using BTAI;
using DG.Tweening;
using Nekoyume.Data.Table;
using UnityEngine;


namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int RewardExp = 0;

        public void InitAI(Data.Table.Monster statsData)
        {
            WalkSpeed = -1.0f;

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
            foreach (var skill in _skills)
            {
                Destroy(skill);
            }
            _skills.Clear();
            // TODO: select skill
            var skillNames = new[]
            {
                statsData.Skill_0,
                statsData.Skill_1,
                statsData.Skill_2,
                statsData.Skill_3
            };
            foreach (var skillName in skillNames)
            {
                var attack = gameObject.AddComponent<Skill.MonsterAttack>();
                if (string.IsNullOrEmpty(skillName)) continue;
                if (attack.Init(skillName))
                {
                    _skills.Add(attack);
                }
            }
        }

        public void InitStats(Data.Table.Monster statsData, int power)
        {
            HP = Mathf.FloorToInt((float)statsData.Health * ((float)power * 0.01f));
            ATK = statsData.Attack;
            DEF = Mathf.FloorToInt((float)statsData.Defense * ((float)power * 0.01f));
            WeightType = statsData.WeightType;
            RewardExp = Mathf.FloorToInt((float)statsData.RewardExp * ((float)power * 0.01f));

            Power = power;

            _hpMax = HP;
        }

        public override void OnDamage(AttackType attackType, int dmg)
        {
            int clacDmg = CalcDamage(attackType, dmg);
            if (clacDmg <= 0)
                return;

            HP -= clacDmg;

            UI.PopupText.Show(
                transform.TransformPoint(0.1f, 1.0f, 0.0f),
                new Vector3(1.0f, 2.0f, 0.0f),
                clacDmg.ToString(),
                Color.yellow,
                new Vector3(0.01f, -0.1f, 0.0f));

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                DG.Tweening.Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.red, 0.1f));
                colorseq.Append(mat.DOColor(Color.white, 0.1f));
            }

            UpdateHpBar();
        }

        override protected void OnDead()
        {
            base.OnDead();
            Event.OnEnemyDead.Invoke(this);
        }
    }
}
