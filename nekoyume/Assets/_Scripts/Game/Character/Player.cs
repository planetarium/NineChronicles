using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anima2D;
using Nekoyume.Data.Table;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Vfx;
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
        private float _range = 1.2f;

        private void Awake()
        {
            Event.OnAttackEnd.AddListener(AttackEnd);
            Event.OnHitEnd.AddListener(HitEnd);
            Event.OnDieEnd.AddListener(DieEnd);
            Inventory = new Item.Inventory();
            _targetTag = Tag.Enemy;
        }

        public override float Speed => 1.0f;

        private void Start()
        {
            _anim = GetComponentInChildren<Animator>();
        }

        public override IEnumerator CoProcessDamage(int dmg, bool critical)
        {
            yield return StartCoroutine(base.CoProcessDamage(dmg, critical));

            var position = transform.TransformPoint(-0.1f, 0.6f, 0.0f);
            var force = new Vector3(-0.02f, 0.4f);
            var txt = dmg.ToString();
            PopUpDmg(position, force, txt, critical);

            Event.OnUpdateStatus.Invoke();
            if (HP <= 0)
            {
                StartCoroutine(Dying());
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

            var pos = transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            VfxFactory.instance.Create<VfxBattleDamage01>(pos).Play();
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
            UpdateSet(model.set);
            InitStats(character);

            _hpBarOffset.Set(-0.22f, -0.61f, 0.0f);
            _castingBarOffset.Set(-0.22f, -0.85f, 0.0f);
            _mpBarOffset.Set(-0.22f, -0.66f, 0.0f);
        }

        private void InitStats(Model.Player character)
        {
            HP = character.currentHP;
            HPMax = character.hp;
            ATK = character.atk;
            DEF = character.def;
            EXP = character.exp;
            Level = character.level;
            EXPMax = character.expMax;
            Inventory = character.inventory;
        }

        public void UpdateSet(SetItem item)
        {
            var itemId = item?.Data.id ?? 0;
            int id;
            // TODO Change Players mesh instead of weapon only.
            if (SetItem.WeaponMap.TryGetValue(itemId, out id))
            {
                var mesh = Resources.Load<SpriteMesh>($"avatar/character_0003/item_{id}");
                _weapon.spriteMesh = mesh;
            }
        }

        public bool TargetInRange(CharacterBase target) =>
            _range > Mathf.Abs(gameObject.transform.position.x - target.transform.position.x);

        public IEnumerator CoGetExp(long exp)
        {
            if (exp > 0)
            {
                PopupText.Show(
                    transform.TransformPoint(-0.6f, 1.0f,0.0f),
                    new Vector3(0.0f,2.0f,0.0f),
                    $"+{exp}"
                );
                var level = model.level;
                model.GetExp(exp);

                if (model.level != level)
                {
                    yield return new WaitForSeconds(0.3f);
                    PopupText.Show(
                        transform.TransformPoint(-0.6f, 1.0f,0.0f),
                        new Vector3(0.0f,2.0f,0.0f),
                        "LEVEL UP"
                    );
                    InitStats(model);

                    UpdateHpBar();
                    
                    AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
                }
                Event.OnUpdateStatus.Invoke();
            }
        }
    }
}
