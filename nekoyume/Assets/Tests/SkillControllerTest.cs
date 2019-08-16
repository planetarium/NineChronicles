using System;
using System.Collections;
using Libplanet;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Model;
using Nekoyume.State;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    public class SkillControllerTest
    {
        private Nekoyume.Game.Character.Player _player;
        private readonly Nekoyume.Data.Table.Elemental.ElementalType[] _elementalTypes;
        private Address _address;
        private AvatarState _avatarState;

        public SkillControllerTest()
        {
            _elementalTypes = (Nekoyume.Data.Table.Elemental.ElementalType[])
                Enum.GetValues(typeof(Nekoyume.Data.Table.Elemental.ElementalType));
        }

        [SetUp]
        public void Setup()
        {
            _address = new Address();
            var agentAddress = new Address();
            _avatarState = new AvatarState(_address, agentAddress);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.Destroy(_player);
        }

        [UnityTest]
        public IEnumerator GetSkillAreaVFX()
        {
            //FIXME Setup이나 Constructor에서 플레이어를 설정하면 파괴되는 MonoSingleton의 상태가 꼬이는 문제가 있음.
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.model.targets.Add(_player.model);
            Assert.NotNull(_player);
            Assert.NotNull(_player.model);

            foreach (var elemental in _elementalTypes)
            {
                var info = new Skill.SkillInfo(_player.model, 0, false, SkillCategory.Area, elemental);
                yield return _player.CoAreaAttack(new []{info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillDoubleVFX()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.model.targets.Add(_player.model);
            Assert.NotNull(_player);
            Assert.NotNull(_player.model);

            foreach (Nekoyume.Data.Table.Elemental.ElementalType elemental in _elementalTypes)
            {
                var info = new Skill.SkillInfo(_player.model, 0, false, SkillCategory.Double, elemental);
                yield return _player.CoDoubleAttack(new []{info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillBlowVFX()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.model.targets.Add(_player.model);
            Assert.NotNull(_player);
            Assert.NotNull(_player.model);

            foreach (Nekoyume.Data.Table.Elemental.ElementalType elemental in _elementalTypes)
            {
                var info = new Skill.SkillInfo(_player.model, 0, false, SkillCategory.Blow, elemental);
                yield return _player.CoBlow(new []{info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillHealVFX()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.model.targets.Add(_player.model);
            Assert.NotNull(_player);
            Assert.NotNull(_player.model);

            foreach (Nekoyume.Data.Table.Elemental.ElementalType elemental in _elementalTypes)
            {
                var info = new Skill.SkillInfo(_player.model, 0, false, SkillCategory.Normal, elemental);
                yield return _player.CoHeal(new []{info});
            }
        }

        [UnityTest]
        public IEnumerator GetSkillVFXWithCreate()
        {
            var go = Game.instance.stage.playerFactory.Create(_avatarState);
            _player = go.GetComponent<Nekoyume.Game.Character.Player>();
            _player.model.targets.Add(_player.model);
            Assert.NotNull(_player);
            Assert.NotNull(_player.model);

            var pool = Game.instance.stage.objectPool;
            var objects = pool.objects["area_l_water"];
            var current = objects.Count;
            foreach (var effect in objects)
            {
                effect.SetActive(true);
            }

            var info = new Skill.SkillInfo(_player.model, 0, false, SkillCategory.Area,
                Nekoyume.Data.Table.Elemental.ElementalType.Water);
            yield return _player.CoAreaAttack(new[] {info});
            Assert.Greater(pool.objects["area_l_water"].Count, current);
        }
    }
}
