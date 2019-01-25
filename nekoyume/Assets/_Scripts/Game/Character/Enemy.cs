using System;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using DG.Tweening;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int DataId = 0;
        public int RewardExp = 0;
        public Guid id;

        public void InitAI(Data.Table.Monster statsData)
        {
            DataId = statsData.Id;
            RunSpeed = -1.0f;

            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);
            _castingBarOffset.Set(-0.0f, -0.33f, 0.0f);

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
                statsData.Skill_0,
                statsData.Skill_1,
                statsData.Skill_2,
                statsData.Skill_3
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

        public void InitStats(Data.Table.Monster statsData, int power)
        {
            HP = Mathf.FloorToInt((float)statsData.Health * ((float)power * 0.01f));
            ATK = statsData.Attack;
            DEF = Mathf.FloorToInt((float)statsData.Defense * ((float)power * 0.01f));
            WeightType = statsData.WeightType;
            RewardExp = Mathf.FloorToInt((float)statsData.RewardExp * ((float)power * 0.01f));

            Power = power;

            _hpMax = HP;
        }

        public override void OnDamage(int dmg)
        {
            base.OnDamage(dmg);

            PopupText.Show(
                transform.TransformPoint(0.12f, 0.5f, 0.0f),
                new Vector3(0.06f, 0.05f, 0.0f),
                dmg.ToString(),
                Color.yellow);

            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                DG.Tweening.Sequence colorseq = DOTween.Sequence();
                colorseq.Append(mat.DOColor(Color.red, 0.1f));
                colorseq.Append(mat.DOColor(Color.white, 0.1f));
            }
        }

        protected override void OnDead()
        {
            DropItem();

            base.OnDead();
            Event.OnEnemyDead.Invoke(this);
        }

        protected void DropItem()
        {
            var selector = new WeightedSelector<int>();
            var tables = this.GetRootComponent<Tables>();
            foreach (var pair in tables.ItemDrop)
            {
                ItemDrop dropData = pair.Value;
                if (DataId != dropData.MonsterId)
                    continue;
                
                if (dropData.Weight <= 0)
                    continue;

                selector.Add(dropData.ItemId, dropData.Weight);
            }

            if (selector.Count <= 0)
                return;

            var dropItemFactory = GetComponentInParent<DropItemFactory>();
            var dropItem = dropItemFactory.Create(selector.Select(), transform.position);
            if (dropItem != null)
            {
                Event.OnGetItem.Invoke(dropItem.GetComponent<DropItem>());
            }
        }

        public void Init(Model.Monster spawnCharacter)
        {
            RunSpeed = -1.0f;
            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);
            _castingBarOffset.Set(-0.0f, -0.33f, 0.0f);
            InitStats(spawnCharacter.data);
            id = spawnCharacter.id;
        }

        private void InitStats(Data.Table.Monster data)
        {
            HP = data.Health;
            ATK = data.Attack;
            DEF = data.Defense;
            WeightType = data.WeightType;
            RewardExp = data.RewardExp;
            Power = 0;
            _hpMax = HP;
        }
    }
}
