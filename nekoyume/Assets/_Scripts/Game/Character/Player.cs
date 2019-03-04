using System;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.UI;
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
        public Weapon weapon = null;
        public Armor armor;
        public Belt belt;
        public Necklace necklace;
        public Ring ring;
        public Helm helm;
        public List<Equipment> equipments =>
            Inventory.items.Select(i => i.Item).OfType<Equipment>().Where(e => e.IsEquipped).ToList();

        protected override Vector3 _hpBarOffset => _castingBarOffset + new Vector3(0, 0.24f, 0.0f);
        protected Vector3 _mpBarOffset => _castingBarOffset + new Vector3(0, 0.19f, 0.0f);

        protected override Vector3 _castingBarOffset
        {
            get
            {
                var face = GetComponentsInChildren<Transform>().First(g => g.name == "face");
                var faceRenderer = face.GetComponent<Renderer>();
                var x = faceRenderer.bounds.min.x - transform.position.x + faceRenderer.bounds.size.x / 2;
                var y = faceRenderer.bounds.max.y - transform.position.y;
                return new Vector3(x, y, 0.0f);
            }
        }

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
            RunSpeed = 0.0f;

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
                            BT.If(() => RunSpeed > 0.0f).OpenBranch(
                                BT.Call(Run)
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

            InitInventory(avatar);
            CalcStats();
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

        public override void OnDamage(int dmg)
        {
            base.OnDamage(dmg);

            PopupText.Show(
                transform.TransformPoint(UnityEngine.Random.Range(-0.6f, -0.4f), 1.0f, 0.0f),
                new Vector3(-0.02f, 0.02f, 0.0f),
                dmg.ToString(),
                Color.red);

            if (HP <= 0)
            {
                Die();
            }
        }

        protected override void OnDead()
        {
            gameObject.SetActive(false);
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

            HPMax = statsData.Health;
            EXPMax = statsData.Exp;
            if (weapon?.IsEquipped == true)
            {
                ATK += weapon.Data.Param_0;
            }

            if (armor?.IsEquipped == true)
            {
                DEF += armor.Data.Param_0;
            }
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
            ActionManager.Instance.UpdateItems(Inventory.items);
        }

        private void InitInventory(Model.Avatar avatar)
        {
            var inventoryItems = avatar.Items;
            if (inventoryItems != null)
            {
                foreach (var inventoryItem in inventoryItems)
                {
                    if (inventoryItem.Item is Weapon)
                    {
                        weapon = (Weapon) inventoryItem.Item;
                    }
                }

                Inventory.Set(inventoryItems);
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

            _mpBar.SetValue((float) MP / (float) MPMax);
        }

        public void Equip(Weapon equipment)
        {
            if (weapon != equipment)
            {
                weapon?.Unequip();
                weapon = equipment;
            }
        }

        public void Equip(Armor equipment)
        {
            if (armor != equipment)
            {
                armor?.Unequip();
                armor = equipment;
            }
        }

        public void Equip(Belt equipment)
        {
            if (belt != equipment)
            {
                belt?.Unequip();
                belt = equipment;
            }

        }

        public void Equip(Necklace equipment)
        {
            if (necklace != equipment)
            {
                necklace?.Unequip();
                necklace = equipment;
            }

        }

        public void Equip(Ring equipment)
        {
            if (ring != equipment)
            {
                ring?.Unequip();
                ring = equipment;
            }
        }

        public void Equip(Helm equipment)
        {
            if (helm != equipment)
            {
                helm?.Unequip();
                helm = equipment;
            }
        }

        public void Init()
        {
            RunSpeed = 0.0f;

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);
            _castingBarOffset.Set(-0.22f, -0.85f, 0.0f);
            _mpBarOffset.Set(-0.22f, -0.66f, 0.0f);
        }

        public void Equip(Equipment equipment)
        {
            if (equipment is Weapon)
            {
                Equip((Weapon) equipment);
            }
            else if (equipment is Armor)
            {
                Equip((Armor) equipment);
            }
            else if (equipment is Belt)
            {
                Equip((Belt) equipment);
            }
            else if (equipment is Necklace)
            {
                Equip((Necklace) equipment);
            }
            else if (equipment is Ring)
            {
                Equip((Ring) equipment);
            }
            else if (equipment is Helm)
            {
                Equip((Helm) equipment);
            }
            equipment.Use();
            CalcStats();
            Event.OnUpdateEquipment.Invoke(equipment);
        }
    }
}
