using System;
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
        public Weapon _weapon = null;

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
            Event.OnSlotClick.AddListener(SlotClick);
            Event.OnEquip.AddListener(Equip);
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
            if (_weapon?.IsEquipped == true)
            {
                ATK += _weapon.Data.Param_0;
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
                        _weapon = (Weapon) inventoryItem.Item;
                    }
                }

                Inventory.Set(inventoryItems);
            }

            //
//            if (!string.IsNullOrEmpty(avatar.Items))
//            {
//                var inventoryItems = JsonConvert.DeserializeObject<List<Item.Inventory.InventoryItem>>(avatar.Items);
//                foreach (var inventoryItem in inventoryItems)
//                {
//                    if (inventoryItem.Item is Weapon)
//                    {
//                        _weapon = (Weapon) inventoryItem.Item;
//                    }
//                }
//
//                Inventory.Set(inventoryItems);
//            }
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

        public void Equip(Equipment equipment)
        {
            if (_weapon != equipment)
            {
                _weapon?.Unequip();
                _weapon = (Weapon) equipment;
            }

            // Equip or UnEquip
            _weapon.Use();
            CalcStats();
            Event.OnUpdateEquipment.Invoke(_weapon);
            // TODO Implement Actions
            ActionManager.Instance.UpdateItems(Inventory.items);
        }

        public void Init()
        {
            RunSpeed = 0.0f;

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);
            _castingBarOffset.Set(-0.22f, -0.85f, 0.0f);
            _mpBarOffset.Set(-0.22f, -0.66f, 0.0f);
        }

        public void SlotClick(InventorySlot slot)
        {
            var item = slot.Item as Weapon;
            if (item != null)
            {
                Event.OnEquip.Invoke(item);
            }
        }
    }
}
