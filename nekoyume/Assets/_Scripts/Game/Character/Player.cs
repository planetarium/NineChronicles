using BTAI;
using Nekoyume.Data;
using Nekoyume.Model;

namespace Nekoyume.Game.Character
{
    public class Player : CharacterBase
    {
        public int MP = 0;
        public int EXP = 0;

        private void Awake()
        {
            Event.OnEnemyDead.AddListener(GetEXP);
        }

        public void InitAI()
        {
            _walkSpeed = 1.0f;
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
            var attack = this.GetOrAddComponent<Skill.Attack>();
            if (attack.Init("attack"))
            {
                _skills.Add(attack);
            }
        }

        public void InitStats(Data.Table.Stats statsData, Avatar avatar)
        {
            HP = (!avatar.dead && avatar.hp > 0) ? avatar.hp : statsData.Health;
            ATK = statsData.Attack;
            DEF = statsData.Defense;
            MP = statsData.Mana;
            EXP = avatar.exp;
        }

        protected override void OnDead()
        {
            Event.OnPlayerDead.Invoke();
        }

        private void GetEXP(Enemy enemy)
        {
            EXP += enemy.RewardExp;
        }

        public string GetLevel()
        {
            var tables = this.GetRootComponent<Tables>();
            return tables.GetLevel(EXP).ToString();
        }
    }
}
