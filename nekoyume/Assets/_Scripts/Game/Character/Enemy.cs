using System;
using System.Linq;
using BTAI;
using DG.Tweening;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int DataId = 0;
        public int RewardExp = 0;
        public Guid id;

        protected override Vector3 _hpBarOffset => _castingBarOffset + new Vector3(0, 0 + 0.22f, 0.0f);

        protected override Vector3 _castingBarOffset
        {
            get
            {
                var spriteRenderer = GetComponentInChildren<Renderer>();
                var x = spriteRenderer.bounds.min.x - transform.position.x + spriteRenderer.bounds.size.x / 2;
                var y = spriteRenderer.bounds.max.y - transform.position.y;
                return new Vector3(x, y, 0.0f);
            }
        }

        public override float Speed => 0.0f;

        public void InitAI(Monster statsData)
        {
            DataId = statsData.Id;
            RunSpeed = -1.0f;

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
                            BT.Call(Run)
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
                statsData.skill0,
                statsData.skill1,
                statsData.skill2,
                statsData.skill3
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

        public void InitStats(Monster statsData, int power)
        {
            HP = Mathf.FloorToInt((float)statsData.Health * ((float)power * 0.01f));
            ATK = statsData.Attack;
            DEF = Mathf.FloorToInt((float)statsData.Defense * ((float)power * 0.01f));
            WeightType = statsData.WeightType;
            RewardExp = Mathf.FloorToInt((float)statsData.RewardExp * ((float)power * 0.01f));

            Power = power;

            HPMax = HP;
        }

        public override void OnDamage(int dmg, bool critical)
        {
            base.OnDamage(dmg, critical);

            var position = transform.TransformPoint(0.1f, 0.8f, 0.0f);
            var force = new Vector3(0.02f, 0.4f);
            var txt = dmg.ToString();
            PopUpDmg(position, force, txt, critical);

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                UnityEngine.Material mat = renderer.material;
                DG.Tweening.Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.red, 0.1f));
                colorseq.Append(mat.DOColor(Color.white, 0.1f));
            }

            if (HP <= 0)
            {
                Die();
            }
        }

        protected override void OnDead()
        {
            Event.OnEnemyDead.Invoke(this);
            base.OnDead();
        }

        public void Init(Model.Monster spawnCharacter)
        {
            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);
            _castingBarOffset.Set(-0.0f, -0.33f, 0.0f);
            InitStats(spawnCharacter.data);
            id = spawnCharacter.id;
            StartRun();
        }

        private void InitStats(Monster data)
        {
            HP = data.Health;
            ATK = data.Attack;
            DEF = data.Defense;
            WeightType = data.WeightType;
            RewardExp = data.RewardExp;
            Power = 0;
            HPMax = HP;
        }

        private void Awake()
        {
            _targetTag = Tag.Player;
        }

    }
}
