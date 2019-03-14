using System;
using System.Collections.Generic;
using System.Linq;
using Anima2D;
using BTAI;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.Game.VFX;
using Nekoyume.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Character
{
    public class Player : CharacterBase
    {
        public int MP = 0;
        public long EXP = 0;
        public int Level = 0;
        public int MPMax = 0;
        [SerializeField] private SpriteMeshInstance _weapon;

        public long EXPMax { get; private set; }

        private ProgressBar _mpBar = null;

        public List<Equipment> equipments =>
            Inventory.items.Select(i => i.Item).OfType<Equipment>().Where(e => e.equipped).ToList();

        public Model.Player model;

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

        public override void OnDamage(int dmg, bool critical)
        {
            base.OnDamage(dmg, critical);

            var position = transform.TransformPoint(UnityEngine.Random.Range(-0.6f, -0.4f), 1.0f, 0.0f);
            var force = new Vector3(-0.02f, 0.02f, 0.0f);
            var txt = dmg.ToString();
            PopUpDmg(position, force, txt, critical);
            
            // 회복 이펙트 테스트를 위해 70%의 확률로 피격 이펙트를 생성하고, 30%의 확률로 회복 이펙트를 생성한다.
            if (Random.value < 0.7f)
            {
                var pos = transform.position;
                pos.x -= 0.2f;
                pos.y += 0.32f;
                VfxFactory.instance.Create<VfxBattleDamage01>(pos).Play();
            }
            else
            {
                var pos = transform.position;
                pos.x -= 0.2f;
                pos.y += 0.32f;
                VfxFactory.instance.Create<VfxBattleHeal01>(pos).Play();
            }

            Event.OnUpdateStatus.Invoke();
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

        protected override void PopUpDmg(Vector3 position, Vector3 force, string dmg, bool critical)
        {
            base.PopUpDmg(position, force, dmg, critical);

            // 피격 이펙트 발동.
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
            model.level++;

            PopupText.Show(transform.TransformPoint(-0.6f, 1.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f), "LEVEL UP");

            model.CalcStats(model.level);
            InitStats(model);

            UpdateHpBar();
        }

        private void PickUpItem(DropItem item)
        {
            Inventory.Add(item.Item);
            ActionManager.Instance.UpdateItems(Inventory.items);
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

        protected override void Update()
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

        public void Init(Model.Player character)
        {
            model = character;
            UpdateWeapon(model.weapon);
            InitStats(character);
            RunSpeed = 0.0f;

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);
            _castingBarOffset.Set(-0.22f, -0.85f, 0.0f);
            _mpBarOffset.Set(-0.22f, -0.66f, 0.0f);
        }

        private void InitStats(Model.Player character)
        {
            HP = character.hp;
            HPMax = character.hpMax;
            ATK = character.atk;
            DEF = character.def;
            EXP = character.exp;
            Level = character.level;
            EXPMax = character.expMax;
            Inventory = character.inventory;
        }

        public void UpdateWeapon(Weapon weapon)
        {
            var mesh = Resources.Load<SpriteMesh>($"avatar/character_0003/item_{weapon?.Data.Id}");
            _weapon.spriteMesh = mesh;
        }
    }
}