using System;
using BTAI;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Model;

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

            _skills.Clear();
            // TODO: select skill
            var attack = this.GetOrAddComponent<Skill.Attack>();
            if (attack.Init("attack"))
            {
                _skills.Add(attack);
            }
        }

        public void InitStats(Avatar avatar)
        {
            EXP = avatar.exp;
            Level = avatar.level;

            CalcStats();

            if (!avatar.dead && avatar.hp > 0)
                HP = avatar.hp;
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
        }

        private void LevelUp()
        {
            if (EXP < EXPMax)
                return;

            EXP -= EXPMax;
            Level++;
            CalcStats();
        }
    }
}
