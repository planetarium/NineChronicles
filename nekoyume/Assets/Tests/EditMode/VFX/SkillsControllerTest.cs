using System;
using Nekoyume.Game.Util;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.VFX
{
    public class SkillControllerTest
    {
        private GameObject _gameObject;
        private ObjectPool.Impl _objectPool;
        private SkillController _skillController;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject();
            _objectPool = new ObjectPool.Impl(_gameObject.transform, null);
            _skillController = new SkillController(_objectPool);
        }

        [Test]
        public void GetSkillCastingVFXTest()
        {
            foreach (var elementalType in (ElementalType[]) Enum.GetValues(typeof(ElementalType)))
            {
                var vfx = _skillController.Get(Vector3.zero, elementalType);
                Assert.IsNotNull(vfx);
            }
        }

        [Test]
        public void GetBlowCastingVFXTest()
        {
            foreach (var elementalType in (ElementalType[]) Enum.GetValues(typeof(ElementalType)))
            {
                if (elementalType == ElementalType.Normal)
                {
                    continue;
                }

                var vfx = _skillController.GetBlowCasting(
                    Vector3.zero,
                    SkillCategory.BlowAttack,
                    elementalType);
                Assert.IsNotNull(vfx);
            }
        }
    }
}
