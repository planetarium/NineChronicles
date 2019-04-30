using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anima2D;
using Nekoyume.Data.Table;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.Vfx;
using Nekoyume.Manager;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Player : CharacterBase
    {
        private static readonly Vector3 DamageTextForce = new Vector3(-0.1f, 0.5f);
        private static readonly Vector3 HpBarOffset = new Vector3(0f, 0.24f);
        private static readonly Vector3 MpBarOffset = new Vector3(0f, 0.19f);
        
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

        protected override Vector3 _hpBarOffset => _castingBarOffset + HpBarOffset;
        protected Vector3 _mpBarOffset => _castingBarOffset + MpBarOffset;

        private const int DefaultSetId = 101000;

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

        protected override void Awake()
        {
            base.Awake();
            
            _anim = GetComponentInChildren<Animator>();
            SetAnimatorSpeed(AnimatorSpeed);
            
            Inventory = new Item.Inventory();
            
            _targetTag = Tag.Enemy;
        }

        public override float Speed => 1.8f;

        public override IEnumerator CoProcessDamage(int dmg, bool critical)
        {
            yield return StartCoroutine(base.CoProcessDamage(dmg, critical));

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;
            var txt = dmg.ToString();
            PopUpDmg(position, force, txt, critical);

            Event.OnUpdateStatus.Invoke();
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
            VfxController.instance.Create<VfxBattleDamage01>(pos).Play();
        }

        protected override void Update()
        {
            base.Update();

            // Reference.
            // if (ReferenceEquals(_anim, null)) 이 라인일 때와 if (_anim == null) 이 라인일 때의 결과가 달라서 주석을 남겨뒀어요.
            // 아마 전자는 포인터가 가리키는 실제 값을 검사하는 것이고, 후자는 _anim의 값을 검사하는 것 같아요.
            // if (ReferenceEquals(_anim, null))
            if (_anim == null)
            {
                _anim = GetComponentInChildren<Animator>();
                SetAnimatorSpeed(AnimatorSpeed);
            }
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
            var itemId = item?.Data.resourceId ?? DefaultSetId;
            var prevAnim = gameObject.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                if (!prevAnim.name.Contains(itemId.ToString()))
                {
                    Destroy(prevAnim.gameObject);
                }
                else
                {
                    return;
                }
            }
            var origin = Resources.Load<GameObject>($"Prefab/{itemId}");

            Instantiate(origin, gameObject.transform);
        }

        public IEnumerator CoGetExp(long exp)
        {
            if (exp <= 0)
            {
                yield break;
            }
            
            PopupText.Show(
                transform.TransformPoint(-0.6f, 1.0f,0.0f),
                new Vector3(0.0f,2.0f,0.0f),
                $"+{exp}"
            );
            var level = model.level;
            model.GetExp(exp);
            EXP += exp;

            if (model.level != level)
            {
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionStatusLevelUp, level);
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
