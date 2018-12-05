using System.Linq;
using BTAI;
using DG.Tweening;
using Nekoyume.Data.Table;
using UnityEngine;


namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        public int DataId = 0;
        public int RewardExp = 0;

        public void InitAI(Monster statsData)
        {
            DataId = statsData.Id;
            WalkSpeed = -1.0f;

            _hpBarOffset.Set(-0.0f, -0.11f, 0.0f);

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
            var tables = this.GetRootComponent<Data.Tables>();
            foreach (var skillName in skillNames)
            {
                Data.Table.Skill skillData;
                if (tables.Skill.TryGetValue(skillName, out skillData))
                {
                    var skillType = typeof(Skill.SkillBase).Assembly
                    .GetTypes()
                    .FirstOrDefault(t => skillData.Cls == t.Name);
                    var skill = gameObject.AddComponent(skillType) as Skill.SkillBase;
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

            _hpMax = HP;
        }

        public override void OnDamage(AttackType attackType, int dmg)
        {
            base.OnDamage(attackType, dmg);

            int calcDmg = CalcDamage(attackType, dmg);

            UI.PopupText.Show(
                transform.TransformPoint(0.1f, 1.0f, 0.0f),
                new Vector3(1.0f, 2.0f, 0.0f),
                calcDmg.ToString(),
                Color.yellow,
                new Vector3(0.01f, -0.1f, 0.0f));

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

        private void DropItem()
        {
            var selector = new Util.WeightedSelector<int>();
            var tables = this.GetRootComponent<Data.Tables>();
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

            var dropItemFactory = GetComponentInParent<Factory.DropItemFactory>();
            var dropItem = dropItemFactory.Create(selector.Select(), transform.position);
            if (dropItem != null)
            {
                Event.OnGetItem.Invoke(dropItem.GetComponent<Item.DropItem>());
            }
        }
    }
}
