using System;
using System.Linq;
using BTAI;
using Nekoyume.Data.Table;
using UnityEngine;


namespace Nekoyume.Game.Character
{
    public class Player : CharacterBase
    {
        public int MP = 0;
        public long EXP = 0;
        public int Level = 0;

        public long EXPMax { get; private set; }

        public override WeightType WeightType
        {
            get { return WeightType.Small; }
            protected set { throw new NotImplementedException(); }
        }

        private void Awake()
        {
            Event.OnEnemyDead.AddListener(GetEXP);
        }

        public void InitAI()
        {
            WalkSpeed = 0.0f;

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);

            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Selector().OpenBranch(
                            BT.If(HasTargetInRange).OpenBranch(
                                BT.Call(Attack)
                            ),
                            BT.If(() => WalkSpeed > 0.0f).OpenBranch(
                                BT.Call(Walk)
                            )
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
            var skillNames = new[]
            {
                "attack",
                "rangedAttack"
            };
            var tables = this.GetRootComponent<Data.Tables>();
            foreach (var skillName in skillNames)
            {
                Data.Table.Skill skillData;
                if (tables.Skill.TryGetValue(skillName, out skillData))
                {
                    var skillType = typeof(Skill.SkillBase).Assembly
                    .GetTypes()
                    .FirstOrDefault(t => skillData.Cls == t.Name);
                    var skill = gameObject.AddComponent(skillType) as Skill.SkillBase;
                    if (skill.Init(skillData))
                    {
                        _skills.Add(skill);
                    }
                }
            }
        }

        public void InitStats(Model.Avatar avatar)
        {
            EXP = avatar.exp;
            Level = avatar.level;

            CalcStats();

            if (!avatar.dead && avatar.hp > 0)
                HP = avatar.hp;
        }

        public override void OnDamage(AttackType attackType, int dmg)
        {
            int clacDmg = CalcDamage(attackType, dmg);
            if (clacDmg <= 0)
                return;

            HP -= clacDmg;

            UI.PopupText.Show(
                transform.TransformPoint(UnityEngine.Random.Range(-0.6f, -0.4f), 1.0f, 0.0f),
                new Vector3(0.0f, 2.0f, 0.0f),
                clacDmg.ToString(),
                Color.red,
                new Vector3(-0.01f, -0.1f, 0.0f));

            UpdateHpBar();
        }
        protected override void OnDead()
        {
            Event.OnPlayerDead.Invoke();
        }

        private void CalcStats()
        {
            Data.Tables tables = this.GetRootComponent<Data.Tables>();
            Data.Table.Stats statsData;
            if (!tables.Stats.TryGetValue(Level, out statsData))
                return;

            HP = statsData.Health;
            ATK = statsData.Attack;
            DEF = statsData.Defense;
            MP = statsData.Mana;

            _hpMax = statsData.Health;
            EXPMax = statsData.Exp;
        }

        private void GetEXP(Enemy enemy)
        {
            EXP += enemy.RewardExp;

            while (EXPMax <= EXP)
            {
                LevelUp();
            }

            Event.OnUpdateStatus.Invoke();
        }

        private void LevelUp()
        {
            if (EXP < EXPMax)
                return;

            EXP -= EXPMax;
            Level++;

            UI.PopupText.Show(transform.TransformPoint(-0.6f, 1.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f), "LEVEL UP");

            CalcStats();

            UpdateHpBar();
        }
    }
}
