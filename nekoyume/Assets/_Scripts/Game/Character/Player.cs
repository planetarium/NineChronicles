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
        public int MPMax = 0;

        public long EXPMax { get; private set; }

        private ProgressBar _mpBar = null;
        private Vector3 _mpBarOffset = new Vector3();

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

        private void Start()
        {
            _anim = GetComponentInChildren<Animator>();
        }

        public void InitAI()
        {
            WalkSpeed = 0.0f;

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);
            _castingBarOffset.Set(-0.22f, -0.85f, 0.0f);
            _mpBarOffset.Set(-0.22f, -0.66f, 0.0f);

            Root = new Root();
            Root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(IsAlive).OpenBranch(
                        BT.Selector().OpenBranch(
                            BT.Condition(() => Casting),
                            BT.If(() => CastedSkill != null).OpenBranch(
                                BT.Call(() => UseSkill(CastedSkill, false))
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
            EXP = avatar.EXP;
            Level = avatar.Level;

            CalcStats();
            InitInventory(avatar);

            if (!avatar.Dead && avatar.CurrentHP > 0)
                HP = avatar.CurrentHP;
        }

        protected override void Attack()
        {
            foreach (var skill in _skills)
            {
                UseSkill(skill);
            }
        }

        public override bool UseSkill(SkillBase selectedSkill, bool checkRange = true)
        {
            bool used = base.UseSkill(selectedSkill, checkRange);
            if (used)
            {
                MP -= selectedSkill.Data.Cost;
                UpdateHpBar();
                UpdateMpBar();
                Event.OnUseSkill.Invoke();
            }
            return used;
        }

        public override bool CancelCast()
        {
            bool canceled = base.CancelCast();
            if (canceled)
                Event.OnUseSkill.Invoke();
            return canceled;
        }

        public override void OnDamage(AttackType attackType, int dmg)
        {
            base.OnDamage(attackType, dmg);

            int calcDmg = CalcDamage(attackType, dmg);

            PopupText.Show(
                transform.TransformPoint(UnityEngine.Random.Range(-0.6f, -0.4f), 1.0f, 0.0f),
                new Vector3(-0.02f, 0.02f, 0.0f),
                calcDmg.ToString(),
                Color.red);
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
            MP = MPMax = statsData.Mana;

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
            if (!string.IsNullOrEmpty(avatar.Items))
            {
                var items = JsonConvert.DeserializeObject<List<Item.Inventory.InventoryItem>>(avatar.Items);
                Inventory.Set(items);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_mpBar != null)
            {
                Destroy(_mpBar.gameObject);
                _mpBar = null;
            }
        }

        private void Update()
        {
            base.Update();
            if (_mpBar != null)
            {
                _mpBar.UpdatePosition(gameObject, _mpBarOffset);
            }
        }

        public void UpdateMpBar()
        {
            if (_mpBar == null)
            {
                _mpBar = Widget.Create<ProgressBar>(true);
                _mpBar.greenBar = Resources.Load<Sprite>("ui/UI_bar_01_blue");
            }
            _mpBar.SetValue((float)MP / (float)MPMax);
        }
    }
}
