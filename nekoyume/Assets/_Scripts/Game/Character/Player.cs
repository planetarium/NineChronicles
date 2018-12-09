using System;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.UI;
using Newtonsoft.Json;
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

        public Item.Inventory Inventory;
        private void Awake()
        {
            Event.OnEnemyDead.AddListener(GetEXP);
            Event.OnGetItem.AddListener(PickUpItem);
            Inventory = new Item.Inventory();
        }

        public void InitAI()
        {
            WalkSpeed = 0.0f;

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);
            _castingBarOffset.Set(-0.22f, -0.83f, 0.0f);

            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Selector().OpenBranch(
                            BT.If(() => Casting).OpenBranch(
                                BT.Call(() => { })
                            ),
                            BT.If(() => CastedSkill != null).OpenBranch(
                                BT.Call(() => UseSkill(CastedSkill))
                            ),
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
            var tables = this.GetRootComponent<Tables>();
            foreach (var skillName in skillNames)
            {
                Data.Table.Skill skillData;
                if (tables.Skill.TryGetValue(skillName, out skillData))
                {
                    var skillType = typeof(SkillBase).Assembly
                        .GetTypes()
                        .FirstOrDefault(t => skillData.Cls == t.Name);
                    var skill = gameObject.AddComponent(skillType) as SkillBase;
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
            InitInventory(avatar);

            if (!avatar.dead && avatar.hp > 0)
                HP = avatar.hp;
        }

        protected override void Attack()
        {
            bool used = TryAttack();
            if (used)
            {
                Event.OnUseSkill.Invoke();
            }
        }

        public override bool UseSkill(SkillBase selectedSkill)
        {
            bool used = base.UseSkill(selectedSkill);
            if (used)
                Event.OnUseSkill.Invoke();
            return used;
        }

        public override void OnDamage(AttackType attackType, int dmg)
        {
            bool casting = Casting;
            base.OnDamage(attackType, dmg);
            if (casting && !Casting)
                Event.OnUseSkill.Invoke();

            int calcDmg = CalcDamage(attackType, dmg);

            PopupText.Show(
                transform.TransformPoint(UnityEngine.Random.Range(-0.6f, -0.4f), 1.0f, 0.0f),
                new Vector3(0.0f, 2.0f, 0.0f),
                calcDmg.ToString(),
                Color.red,
                new Vector3(-0.01f, -0.1f, 0.0f));
        }

        public string SerializeItems()
        {
            var items = JsonConvert.SerializeObject(Inventory._items);
            Inventory._items.Clear();
            return items;
        }

        protected override void OnDead()
        {
            Event.OnPlayerDead.Invoke();
        }

        private void CalcStats()
        {
            Tables tables = this.GetRootComponent<Tables>();
            Stats statsData;
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

            PopupText.Show(transform.TransformPoint(-0.6f, 1.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f), "LEVEL UP");

            CalcStats();

            UpdateHpBar();
        }

        private void PickUpItem(DropItem item)
        {
            Inventory.Add(item.Item);
        }

        private void InitInventory(Model.Avatar avatar)
        {
            if (!string.IsNullOrEmpty(avatar.items))
            {
                var items = JsonConvert.DeserializeObject<List<Item.Inventory.InventoryItem>>(avatar.items);
                Inventory.Set(items);
            }
        }
    }
}
