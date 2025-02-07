using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Util;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class SkillController
    {
#if UNITY_ANDROID || UNITY_IOS
        private const int InitCount = 1;
#else
        private const int InitCount = 5;
#endif

        private IObjectPool _pool;
        public async UniTask InitializeAsync(IObjectPool objectPool)
        {
            _pool = objectPool;
            // TODO: SkillVfx 상속받은 프리팹만 로드하도록 에셋 구분
            await ResourceManager.Instance.LoadAllAsync<GameObject>(ResourceManager.SkillLabel, true, assetAddress =>
            {
                var prefab = ResourceManager.Instance.Load<GameObject>(assetAddress);
                if (prefab == null)
                {
                    NcDebug.LogError($"Failed to load {assetAddress}");
                    return;
                }
                _pool.Add(prefab, InitCount);
            });
        }

        [CanBeNull]
        public T Get<T>(Character.Character target, Model.BattleStatus.Skill.SkillInfo skillInfo)
            where T : SkillVFX
        {
            if (target is null)
            {
                return null;
            }

            var position = target.transform.position;
            var size = target.SizeType == SizeType.XS ? SizeType.S : SizeType.M;
            var elemental = skillInfo.ElementalType;
            if (skillInfo.SkillCategory == SkillCategory.AreaAttack)
            {
                size = SizeType.L;
                //FIXME 현재 무속성 범위공격 이펙트는 존재하지 않기때문에 임시처리.
                if (elemental == ElementalType.Normal)
                {
                    elemental = ElementalType.Fire;
                }

                var pos = ActionCamera.instance.Cam.ScreenToWorldPoint(
                    new Vector2((float)Screen.width / 2, 0));
                position.x = pos.x + 0.5f;
                position.y = Stage.StageStartPosition;
            }

            var skillName = $"{skillInfo.SkillCategory}_{size}_{elemental}".ToLower();
            if (skillInfo.SkillCategory == SkillCategory.BlowAttack &&
                skillInfo.SkillTargetType == SkillTargetType.Enemies)
            {
                skillName = $"{skillInfo.SkillCategory}_m_{elemental}_area".ToLower();
            }
            else
            {
                position.x -= 0.2f;
                position.y += 0.32f;
            }

            var go = _pool.Get(skillName, false, position) ??
                _pool.Get(skillName, true, position);

            return go == null ? null : GetEffect<T>(go, target);
        }

        public T Get<T>(Character.Character target, ArenaSkill.ArenaSkillInfo skillInfo)
            where T : SkillVFX
        {
            if (target is null)
            {
                return null;
            }

            var position = target.transform.position;
            var size = target.SizeType == SizeType.XS ? SizeType.S : SizeType.M;
            var elemental = skillInfo.ElementalType;
            if (skillInfo.SkillCategory == SkillCategory.AreaAttack)
            {
                size = SizeType.L;
                //FIXME 현재 무속성 범위공격 이펙트는 존재하지 않기때문에 임시처리.
                if (elemental == ElementalType.Normal)
                {
                    elemental = ElementalType.Fire;
                }

                var pos = ActionCamera.instance.Cam.ScreenToWorldPoint(
                    new Vector2((float)Screen.width / 2, 0));
                position.x = pos.x + 0.5f;
                position.y = Stage.StageStartPosition;
            }

            var skillName = $"{skillInfo.SkillCategory}_{size}_{elemental}".ToLower();
            if (skillInfo.SkillCategory == SkillCategory.BlowAttack &&
                skillInfo.SkillTargetType == SkillTargetType.Enemies)
            {
                skillName = $"{skillInfo.SkillCategory}_m_{elemental}_area".ToLower();
            }
            else
            {
                position.x -= 0.2f;
                position.y += 0.32f;
            }

            var go = _pool.Get(skillName, false, position) ??
                _pool.Get(skillName, true, position);

            return GetEffect<T>(go, target);
        }

        public SkillCastingVFX Get(Vector3 position, ElementalType elementalType)
        {
            var skillName = $"casting_{elementalType}".ToLower();
            var go = _pool.Get(skillName, false, position) ??
                _pool.Get(skillName, true, position);

            return GetEffect<SkillCastingVFX>(go);
        }

        public SkillCastingVFX GetBlowCasting(
            Vector3 position,
            SkillCategory skillCategory,
            ElementalType elementalType)
        {
            var skillName =
                $"casting_{skillCategory}_{elementalType}".ToLower();
            var go = _pool.Get(skillName, false, position) ??
                _pool.Get(skillName, true, position);

            return GetEffect<SkillCastingVFX>(go);
        }

        private static T GetEffect<T>(GameObject go, Character.Character target = null)
            where T : SkillVFX
        {
            var effect = go.GetComponent<T>();
            if (effect is null)
            {
                throw new NotFoundComponentException<T>(go.name);
            }

            if (target is not null)
            {
                effect.target = target;
            }

            effect.Stop();
            return effect;
        }

        public T Get<T>(GameObject target, ElementalType elemental, SkillCategory skillCategory, SkillTargetType skillTargetType) where T : SkillVFX
        {
            var position = target.transform.position;
            var size = "m";
            if (skillCategory == SkillCategory.AreaAttack)
            {
                size = "l";
                var pos = ActionCamera.instance.Cam.ScreenToWorldPoint(
                    new Vector2((float)Screen.width / 2, 0));
                position.x = pos.x + 0.5f;
                position.y = Stage.StageStartPosition;
            }

            var skillName = $"{skillCategory}_{size}_{elemental}".ToLower();
            if (skillCategory == SkillCategory.BlowAttack &&
                skillTargetType == SkillTargetType.Enemies)
            {
                skillName = $"{skillCategory}_m_{elemental}_area".ToLower();
            }
            else
            {
                position.x -= 0.2f;
                position.y += 0.32f;
            }

            var go = _pool.Get(skillName, false, position) ??
                _pool.Get(skillName, true, position);

            return GetEffect<T>(go, target);
        }

        private static T GetEffect<T>(GameObject go, GameObject target)
            where T : SkillVFX
        {
            var effect = go.GetComponent<T>();
            if (effect is null)
            {
                throw new NotFoundComponentException<T>(go.name);
            }

            if (!(target is null))
            {
                effect.go = target;
            }

            effect.Stop();
            return effect;
        }
    }
}
